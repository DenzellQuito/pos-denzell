using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;
using System.Text.Json;
using POS_DENZELL.DTOs;

namespace POS_DENZELL.Services
{
    // Definimos el Contrato (Interfaz)
    public interface IVentaService
    {
        Task<int> RegistrarVentaAsync(int idTurno, int idUsuario, decimal efectivo, decimal yape, string carritoJson, IFormFile? fotoYape, string? numeroOperacion);
        Task<TicketVentaDTO> ObtenerTicketVentaAsync(int idVenta);
    }

    // Implementamos la Lógica
    public class VentaService : IVentaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VentaService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<int> RegistrarVentaAsync(int idTurno, int idUsuario, decimal efectivo, decimal yape, string carritoJson, IFormFile? fotoYape, string? numeroOperacion)
        {
            // Validar Carrito
            var carrito = JsonSerializer.Deserialize<List<CarritoItemDTO>>(carritoJson);
            if (carrito == null || !carrito.Any()) throw new Exception("El carrito está vacío.");

            //  REDONDEO COMERCIAL EXACTO A 2 DECIMALES (Estándar SUNAT)
            decimal sumaBruta = carrito.Sum(i => i.precio * i.cantidad);
            // Sumamos los subtotales de cada fila ya redondeados a 2 decimales 
            decimal montoTotal = carrito.Sum(i => Math.Round(i.precio * i.cantidad, 2, MidpointRounding.AwayFromZero));

            decimal vuelto = Math.Round((efectivo + yape) - montoTotal, 2, MidpointRounding.AwayFromZero);
            if (vuelto < 0) vuelto = 0;

            // Calculamos la base imponible en 2 decimales
            decimal subtotalSinIgv = Math.Round(montoTotal / 1.18m, 2, MidpointRounding.AwayFromZero);
            decimal totalIgv = montoTotal - subtotalSinIgv;

            
            string? rutaFoto = null;
            string? rutaFisicaFoto = null;

            if (fotoYape != null && fotoYape.Length > 0)
            {
                var extension = Path.GetExtension(fotoYape.FileName).ToLowerInvariant();
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!extensionesPermitidas.Contains(extension))
                {
                    throw new Exception("Formato de comprobante no permitido. Solo se aceptan imágenes JPG, PNG o WEBP.");
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "fotos_yape");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Si no escribieron número de operación, usamos "SINOP"
                string opLimpia = string.IsNullOrWhiteSpace(numeroOperacion)
                    ? "SINOP"
                    : numeroOperacion.Trim();

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"YAPE_{timestamp}_{opLimpia}{extension}";

                rutaFisicaFoto = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(rutaFisicaFoto, FileMode.Create))
                {
                    await fotoYape.CopyToAsync(fileStream);
                }

                rutaFoto = "/fotos_yape/" + uniqueFileName;
            }

            // Iniciar Transacción de Base de Datos (Seguridad Financiera)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generamos el correlativo dentro de la transacción para evitar duplicados
                string serieTicket = "T001";
                int ultimoCorrelativo = await _context.Ventas
                    .Where(v => v.Serie == serieTicket)
                    .MaxAsync(v => (int?)v.Correlativo) ?? 0;

                int nuevoCorrelativo = ultimoCorrelativo + 1;

                var nuevaVenta = new Venta
                {
                    IdTurno = idTurno,
                    IdUsuario = idUsuario,
                    FechaHora = DateTime.Now,
                    TipoDocumento = "TK",
                    Serie = serieTicket,
                    Correlativo = nuevoCorrelativo,
                    MontoTotal = montoTotal,
                    TotalIGV = totalIgv,
                    EstadoSunat = "Local",
                    PagoEfectivo = efectivo,
                    PagoYape = yape,
                    Vuelto = vuelto,
                    RutaFotoComprobante = rutaFoto,
                    NumeroOperacion = string.IsNullOrWhiteSpace(numeroOperacion) ? null : numeroOperacion.Trim(),
                    
                };

                _context.Ventas.Add(nuevaVenta);
                await _context.SaveChangesAsync();

                foreach (var item in carrito)
                {
                    var detalle = new VentaDetalle
                    {
                        IdVenta = nuevaVenta.IdVenta,
                        IdProducto = item.id,
                        Cantidad = item.cantidad,
                        PrecioUnitario = item.precio, // El unitario sí conserva sus 3 decimales (1.917)
                        SubTotal = Math.Round(item.precio * item.cantidad, 2, MidpointRounding.AwayFromZero) // El subtotal de fila va a 2 decimales (23.00)
                    };
                    _context.VentaDetalles.Add(detalle);

                    var producto = await _context.Productos.FindAsync(item.id);
                    if (producto != null) producto.Stock -= item.cantidad;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return nuevaVenta.IdVenta;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                // Si la BD falló pero se había subido una foto de Yape, la borramos para no dejar basura
                if (!string.IsNullOrEmpty(rutaFisicaFoto) && File.Exists(rutaFisicaFoto))
                {
                    File.Delete(rutaFisicaFoto);
                }

                throw new Exception("Ocurrió un error al guardar la venta en la base de datos.");
            }
        }

        // Implementación de la búsqueda del ticket
        public async Task<TicketVentaDTO> ObtenerTicketVentaAsync(int idVenta)
        {
            var venta = await _context.Ventas.FindAsync(idVenta);
            if (venta == null) throw new Exception("Venta no encontrada.");

            var cajero = await _context.Usuarios.FindAsync(venta.IdUsuario);

            var detalles = await (from d in _context.VentaDetalles
                                  join p in _context.Productos on d.IdProducto equals p.IdProducto
                                  where d.IdVenta == idVenta
                                  select new TicketDetalleDTO
                                  {
                                      Cantidad = d.Cantidad,
                                      Descripcion = p.Descripcion,
                                      PrecioUnitario = d.PrecioUnitario,
                                      Subtotal = d.SubTotal
                                  }).ToListAsync();

            return new TicketVentaDTO
            {
                IdVenta = venta.IdVenta,
                FechaHora = venta.FechaHora,
                NombreCajero = cajero?.Nombre ?? "Cajero",
                MontoTotal = venta.MontoTotal,
                PagoEfectivo = venta.PagoEfectivo,
                PagoYape = venta.PagoYape,
                Vuelto = venta.Vuelto,
                TotalIGV = venta.TotalIGV,
                ClienteDocumento = venta.ClienteDocumento,
                ClienteNombre = venta.ClienteNombre,
                Detalles = detalles,
                NumeroComprobante = $"{venta.Serie}-{venta.Correlativo:D6}"
            };
        }
    }
}