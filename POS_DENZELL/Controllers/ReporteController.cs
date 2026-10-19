using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Services;

namespace POS_DENZELL.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReporteController : Controller
    {
        private readonly IReporteService _reporteService;

        public ReporteController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string periodo = "mes", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            DateTime hoy = DateTime.Today;
            DateTime inicio;
            DateTime fin = hoy;

            // Si el usuario usó el selector de fechas personalizado
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                inicio = fechaInicio.Value.Date;
                fin = fechaFin.Value.Date;
                if (inicio > fin) (inicio, fin) = (fin, inicio); // Evita errores si invierten las fechas
                periodo = "personalizado";
            }
            else
            {
                // Botones rápidos de 1 clic
                switch (periodo.ToLower())
                {
                    case "hoy":
                        inicio = hoy;
                        fin = hoy;
                        break;
                    case "semana":
                        inicio = hoy.AddDays(-6); // Últimos 7 días incluido hoy
                        fin = hoy;
                        break;
                    case "anio":
                        inicio = new DateTime(hoy.Year, 1, 1);
                        fin = hoy;
                        break;
                    case "mes":
                    default:
                        inicio = new DateTime(hoy.Year, hoy.Month, 1);
                        fin = hoy;
                        periodo = "mes";
                        break;
                }
            }

            var modeloBi = await _reporteService.ObtenerReporteBiAsync(inicio, fin, periodo);
            return View(modeloBi);
        }
    }
}