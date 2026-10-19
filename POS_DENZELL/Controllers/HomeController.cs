using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Services;

namespace POS_DENZELL.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ICajaService _cajaService;
        private readonly IProductoService _productoService;

        public HomeController(ICajaService cajaService, IProductoService productoService)
        {
            _cajaService = cajaService;
            _productoService = productoService;
        }

        public async Task<IActionResult> Index()
        {
            var turnoAbierto = await _cajaService.ObtenerTurnoAbiertoAsync();
            ViewBag.CajaAbierta = turnoAbierto != null;

            if (turnoAbierto != null)
            {
                ViewBag.IdTurno = turnoAbierto.IdTurno;
                ViewBag.NombreCajero = turnoAbierto.UsuarioCajero?.Nombre ?? "Usuario";
                ViewBag.HoraApertura = turnoAbierto.FechaHoraApertura.ToString("hh:mm tt");

                // se trae el BI del turno 
                var resumen = await _cajaService.ObtenerResumenTurnoAsync(turnoAbierto.IdTurno);
                ViewBag.MontoInicial = resumen.MontoInicial;
                ViewBag.TotalVentas = resumen.TotalVentas;
                ViewBag.IngresosEfectivo = resumen.IngresosEfectivo;
                ViewBag.IngresosYape = resumen.IngresosYape;
                ViewBag.Gastos = resumen.Gastos;
                ViewBag.EfectivoEnGaveta = resumen.EfectivoEsperadoFisico;
                ViewBag.CantidadTickets = resumen.CantidadTickets;
            }

            // Alerta de Stock Crítico
            ViewBag.StockCritico = await _productoService.ObtenerStockCriticoAsync();

            return View();
        }
    }
}