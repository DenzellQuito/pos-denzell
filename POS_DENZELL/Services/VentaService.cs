using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;
using System.Text.Json;
using POS_DENZELL.DTOs;

namespace POS_DENZELL.Services
{
    public interface IVentaService
    {
        Task<int> RegistrarVentaAsync(int idTurno, int idUsuario, decimal efectivo, decimal yape, string carritoJson, IFormFile? fotoYape, string? numeroOperacion, int? idCliente = null, string tipoComprobante = "TK");
        Task<TicketVentaDTO> ObtenerTicketVentaAsync(int idVenta);
    }

    public class VentaService : IVentaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IFacturacionSunatService _facturacionSunat;

        public VentaService(ApplicationDbContext context, IWebHostEnvironment env, IFacturacionSunatService facturacionSunat)
        {
            _context = context;
            _env = env;
            _facturacionSunat = facturacionSunat;
        }

        public async Task<int> RegistrarVentaAsync(
            int idTurno,
            int idUsuario,
            decimal efectivo,
            decimal yape,
            string carritoJson,
            IFormFile? fotoYape,
            string? numeroOperacion,
            int? idCliente = null,
            string tipoComprobante = "TK")
        {
            var carrito = JsonSerializer.Deserialize<List<CarritoItemDTO>>(carritoJson);
            if (carrito == null || !carrito.Any()) throw new Exception("El carrito está vacío.");

            decimal montoTotal = carrito.Sum(i => Math.Round(i.precio * i.cantidad, 2, MidpointRounding.AwayFromZero));

            decimal vuelto = Math.Round((efectivo + yape) - montoTotal, 2, MidpointRounding.AwayFromZero);
            if (vuelto < 0) vuelto = 0;

            decimal subtotalSinIgv = Math.Round(montoTotal / 1.18m, 2, MidpointRounding.AwayFromZero);
            decimal totalIgv = montoTotal - subtotalSinIgv;

            // Normalizamos el tipo de comprobante ("TK" = Ticket, "03" = Boleta, "01" = Factura)
            string tipoDocNormalizado = (tipoComprobante == "01" || tipoComprobante == "03") ? tipoComprobante : "TK";
            string serieComprobante = tipoDocNormalizado switch
            {
                "01" => "F001",
                "03" => "B001",
                _ => "T001"
            };

            // Validamos las reglas de negocio de SUNAT con el cliente seleccionado
            Cliente? cliente = null;
            if (idCliente.HasValue && idCliente.Value > 0)
            {
                cliente = await _context.Clientes.FindAsync(idCliente.Value);
            }

            if (tipoDocNormalizado == "01")
            {
                if (cliente == null)
                    throw new Exception("Para emitir una FACTURA ELECTRÓNICA es obligatorio seleccionar un cliente con RUC.");

                string rucLimpio = cliente.NumeroDocumento.Trim();
                if (cliente.TipoDocumento != "RUC" || rucLimpio.Length != 11 || (!rucLimpio.StartsWith("10") && !rucLimpio.StartsWith("20")))
                    throw new Exception($"El cliente seleccionado ({cliente.NumeroDocumento}) no tiene un RUC válido de 11 dígitos para emitir Factura.");
            }
            else if (tipoDocNormalizado == "03" && montoTotal > 700m)
            {
                if (cliente == null || string.IsNullOrWhiteSpace(cliente.NumeroDocumento))
                    throw new Exception("Por normativa SUNAT, las Boletas de Venta mayores a S/ 700.00 requieren identificar al cliente con su DNI.");
            }

            string? rutaFoto = null;
            string? rutaFisicaFoto = null;

            if (fotoYape != null && fotoYape.Length > 0)
            {
                var extension = Path.GetExtension(fotoYape.FileName).ToLowerInvariant();
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" };

                if (!extensionesPermitidas.Contains(extension))
                {
                    throw new Exception("Formato de comprobante no permitido. Solo se aceptan imágenes JPG, PNG, WEBP o HEIC.");
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "fotos_yape");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string opLimpia = string.IsNullOrWhiteSpace(numeroOperacion) ? "SINOP" : numeroOperacion.Trim();
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"YAPE_{timestamp}_{opLimpia}{extension}";

                rutaFisicaFoto = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(rutaFisicaFoto, FileMode.Create))
                {
                    await fotoYape.CopyToAsync(fileStream);
                }

                rutaFoto = "/fotos_yape/" + uniqueFileName;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generamos el correlativo independiente por Serie (T001, B001 o F001)
                int ultimoCorrelativo = await _context.Ventas
                    .Where(v => v.Serie == serieComprobante)
                    .MaxAsync(v => (int?)v.Correlativo) ?? 0;

                int nuevoCorrelativo = ultimoCorrelativo + 1;

                var nuevaVenta = new Venta
                {
                    IdTurno = idTurno,
                    IdUsuario = idUsuario,
                    FechaHora = DateTime.Now,
                    TipoDocumento = tipoDocNormalizado,
                    Serie = serieComprobante,
                    Correlativo = nuevoCorrelativo,
                    MontoTotal = montoTotal,
                    TotalIGV = totalIgv,
                    EstadoSunat = tipoDocNormalizado == "TK" ? "Local" : "Pendiente",
                    PagoEfectivo = efectivo,
                    PagoYape = yape,
                    Vuelto = vuelto,
                    RutaFotoComprobante = rutaFoto,
                    NumeroOperacion = string.IsNullOrWhiteSpace(numeroOperacion) ? null : numeroOperacion.Trim(),
                    IdCliente = cliente?.IdCliente,
                    ClienteDocumento = cliente?.NumeroDocumento,
                    ClienteNombre = cliente?.NombreCompleto
                };

                _context.Ventas.Add(nuevaVenta);
                await _context.SaveChangesAsync();

                var itemsParaXml = new List<(VentaDetalle Detalle, string DescripcionProducto, string? CodigoBarras)>();

                foreach (var item in carrito)
                {
                    var detalle = new VentaDetalle
                    {
                        IdVenta = nuevaVenta.IdVenta,
                        IdProducto = item.id,
                        Cantidad = item.cantidad,
                        PrecioUnitario = item.precio,
                        SubTotal = Math.Round(item.precio * item.cantidad, 2, MidpointRounding.AwayFromZero)
                    };
                    _context.VentaDetalles.Add(detalle);

                    var producto = await _context.Productos.FindAsync(item.id);
                    if (producto != null)
                    {
                        producto.Stock -= item.cantidad;
                        itemsParaXml.Add((detalle, producto.Descripcion, producto.CodigoBarras));
                    }
                }

                // Emitimos el comprobante al entorno de pruebas SUNAT (si es Boleta "03" o Factura "01")
                if (tipoDocNormalizado != "TK")
                {
                    var respuestaSunat = await _facturacionSunat.EmitirComprobanteBetaAsync(nuevaVenta, itemsParaXml);
                    nuevaVenta.EstadoSunat = respuestaSunat.EstadoSunat;
                    nuevaVenta.CodigoHashSunat = respuestaSunat.CodigoHash;
                    nuevaVenta.MensajeSunat = respuestaSunat.Mensaje;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return nuevaVenta.IdVenta;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                if (!string.IsNullOrEmpty(rutaFisicaFoto) && File.Exists(rutaFisicaFoto))
                {
                    File.Delete(rutaFisicaFoto);
                }

                throw;
            }
        }

        public async Task<TicketVentaDTO> ObtenerTicketVentaAsync(int idVenta)
        {
            var venta = await _context.Ventas.FindAsync(idVenta);
            if (venta == null) throw new Exception("Venta no encontrada.");

            var cajero = await _context.Usuarios.FindAsync(venta.IdUsuario);

            string? direccionCliente = null;
            if (venta.IdCliente.HasValue)
            {
                var cliente = await _context.Clientes.FindAsync(venta.IdCliente.Value);
                direccionCliente = cliente?.Direccion;
            }

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
                TipoDocumento = venta.TipoDocumento,
                Serie = venta.Serie,
                Correlativo = venta.Correlativo,
                EstadoSunat = venta.EstadoSunat,
                CodigoHashSunat = venta.CodigoHashSunat,
                FechaHora = venta.FechaHora,
                NombreCajero = cajero?.Nombre ?? "Cajero",
                MontoTotal = venta.MontoTotal,
                PagoEfectivo = venta.PagoEfectivo,
                PagoYape = venta.PagoYape,
                Vuelto = venta.Vuelto,
                TotalIGV = venta.TotalIGV,
                ClienteDocumento = venta.ClienteDocumento,
                ClienteNombre = venta.ClienteNombre,
                ClienteDireccion = direccionCliente,
                Detalles = detalles,
                NumeroComprobante = $"{venta.Serie}-{venta.Correlativo:D8}"
            };
        }
    }
}