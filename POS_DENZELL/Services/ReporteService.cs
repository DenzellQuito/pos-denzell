using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.DTOs;

namespace POS_DENZELL.Services
{
    public interface IReporteService
    {
        Task<DashboardBiDTO> ObtenerReporteBiAsync(DateTime fechaInicio, DateTime fechaFin, string periodo);
    }

    public class ReporteService : IReporteService
    {
        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardBiDTO> ObtenerReporteBiAsync(DateTime fechaInicio, DateTime fechaFin, string periodo)
        {
            // Ajustamos el rango para cubrir desde las 00:00:00 del inicio hasta las 23:59:59 del día final
            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Traer cabeceras de ventas del periodo
            var ventas = await _context.Ventas
                .Where(v => v.FechaHora >= inicio && v.FechaHora <= fin)
                .ToListAsync();

            // Traer gastos de caja del periodo
            var totalGastos = await _context.MovimientosCaja
                .Where(m => m.FechaHora >= inicio && m.FechaHora <= fin && m.Tipo == "GASTO")
                .SumAsync(m => m.Monto);

            // Traer detalles vendidos en ese rango con su Producto y Categoría
            var detalles = await (from d in _context.VentaDetalles
                                  join v in _context.Ventas on d.IdVenta equals v.IdVenta
                                  join p in _context.Productos on d.IdProducto equals p.IdProducto
                                  join c in _context.Categorias on p.IdCategoria equals c.IdCategoria into catGroup
                                  from c in catGroup.DefaultIfEmpty()
                                  where v.FechaHora >= inicio && v.FechaHora <= fin
                                  select new
                                  {
                                      d.IdProducto,
                                      p.CodigoBarras,
                                      p.Descripcion,
                                      p.EsFiesta,
                                      NombreCategoria = c != null ? c.Nombre : "Sin Categoría",
                                      d.Cantidad,
                                      d.SubTotal
                                  }).ToListAsync();

            // Calcular Ventas por Categoría
            var porCategoria = detalles
                .GroupBy(x => x.NombreCategoria)
                .Select(g => new VentaPorCategoriaDTO
                {
                    Categoria = g.Key,
                    TotalVendido = g.Sum(x => x.SubTotal),
                    UnidadesVendidas = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.TotalVendido)
                .ToList();

            // Calcular Top 10 Productos Estrella
            var topProductos = detalles
                .GroupBy(x => new { x.IdProducto, x.CodigoBarras, x.Descripcion, x.NombreCategoria, x.EsFiesta })
                .Select(g => new ProductoTopDTO
                {
                    CodigoBarras = g.Key.CodigoBarras,
                    Descripcion = g.Key.Descripcion,
                    Categoria = g.Key.NombreCategoria,
                    EsFiesta = g.Key.EsFiesta,
                    CantidadVendida = g.Sum(x => x.Cantidad),
                    TotalGenerado = g.Sum(x => x.SubTotal)
                })
                .OrderByDescending(x => x.TotalGenerado)
                .Take(10)
                .ToList();

            //  Evolución de Ventas Si el filtro es hoy agrupa por Hora; si es varios días agrupa por Día si es "anio" por Mes
            List<VentaPorFechaDTO> evolucion;
            if (periodo == "hoy")
            {
                evolucion = ventas
                    .GroupBy(v => v.FechaHora.Hour)
                    .OrderBy(g => g.Key)
                    .Select(g => new VentaPorFechaDTO
                    {
                        EtiquetaFecha = $"{g.Key:D2}:00",
                        TotalVendido = g.Sum(v => v.MontoTotal)
                    }).ToList();
            }
            else if (periodo == "anio")
            {
                evolucion = ventas
                    .GroupBy(v => new { v.FechaHora.Year, v.FechaHora.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new VentaPorFechaDTO
                    {
                        EtiquetaFecha = $"{g.Key.Month:D2}/{g.Key.Year}",
                        TotalVendido = g.Sum(v => v.MontoTotal)
                    }).ToList();
            }
            else
            {
                evolucion = ventas
                    .GroupBy(v => v.FechaHora.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new VentaPorFechaDTO
                    {
                        EtiquetaFecha = g.Key.ToString("dd/MM"),
                        TotalVendido = g.Sum(v => v.MontoTotal)
                    }).ToList();
            }

            // Empaquetar todo en el DTO maestro
            return new DashboardBiDTO
            {
                FechaInicio = inicio,
                FechaFin = fechaFin.Date,
                PeriodoSeleccionado = periodo,
                TotalVentasBrutas = ventas.Sum(v => v.MontoTotal),
                TotalEfectivoNeto = ventas.Sum(v => v.PagoEfectivo - v.Vuelto),
                TotalYape = ventas.Sum(v => v.PagoYape),
                TotalGastosCaja = totalGastos,
                CantidadTickets = ventas.Count,
                TotalVentasFiesta = detalles.Where(d => d.EsFiesta).Sum(d => d.SubTotal),
                UnidadesVendidasFiesta = detalles.Where(d => d.EsFiesta).Sum(d => d.Cantidad),
                VentasPorCategoria = porCategoria,
                EvolucionVentas = evolucion,
                TopProductos = topProductos
            };
        }
    }
}