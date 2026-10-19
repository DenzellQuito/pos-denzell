using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Services;
using System.Security.Claims;
using System.Globalization;

namespace POS_DENZELL.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly IVentaService _ventaService;
        private readonly ICajaService _cajaService;

        public CajaController(IProductoService productoService, IVentaService ventaService, ICajaService cajaService)
        {
            _productoService = productoService;
            _ventaService = ventaService;
            _cajaService = cajaService;
        }

        // EVITA ERRORES DE FORMATO AL PARSEAR DECIMALES 
        private decimal ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;

            string valorLimpio = valor.Trim().Replace(",", ".");
            return decimal.Parse(valorLimpio, CultureInfo.InvariantCulture);
        }

        // MUESTRA EL FORMULARIO DE APERTURA
        [HttpGet]
        public async Task<IActionResult> Apertura()
        {
            var cajaAbierta = await _cajaService.ObtenerTurnoAbiertoAsync();
            if (cajaAbierta != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // RECIBE EL DINERO INICIAL Y ABRE EL TURNO
        [HttpPost]
        public async Task<IActionResult> Apertura(string MontoInicial)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int idUsuario = int.Parse(userIdString!);

                decimal valMontoInicial = ParsearDecimal(MontoInicial);

                await _cajaService.AbrirTurnoAsync(idUsuario, valMontoInicial);

                return RedirectToAction("Index", "Home");
            }
            catch (FormatException)
            {
                // Error del usuario: monto mal escrito
                ModelState.AddModelError("MontoInicial", "El monto ingresado no es válido.");
                return View();
            }
            catch (InvalidOperationException ex)
            {
                // Error de negocio: por ejemplo, ya hay un turno abierto
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // LA PANTALLA PRINCIPAL DEL PUNTO DE VENTA
        [HttpGet]
        public async Task<IActionResult> PuntoDeVenta()
        {
            var cajaAbierta = await _cajaService.ObtenerTurnoAbiertoAsync();
            if (cajaAbierta == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.IdTurno = cajaAbierta.IdTurno;

            var productos = await _productoService.ObtenerTodosAsync();
            ViewBag.Categorias = await _productoService.ObtenerCategoriasActivasAsync();

            return View(productos);
        }

        // PROCESAR VENTA
        [HttpPost]
        public async Task<IActionResult> ProcesarVenta(
            [FromForm] int IdTurno,
            [FromForm] string Efectivo,
            [FromForm] string Yape,
            [FromForm] string CarritoJson,
            [FromForm] IFormFile? FotoYape,
            [FromForm] string? NumeroOperacion)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int idUsuario = int.Parse(userIdString!);

                decimal valEfectivo = ParsearDecimal(Efectivo);
                decimal valYape = ParsearDecimal(Yape);

                int idVentaGenerada = await _ventaService.RegistrarVentaAsync(
                    IdTurno, idUsuario, valEfectivo, valYape, CarritoJson, FotoYape, NumeroOperacion);

                return Ok(new { success = true, mensaje = "Venta registrada con éxito", idVenta = idVentaGenerada });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        // PANTALLA DE CIERRE DE CAJA 
        [HttpGet]
        public async Task<IActionResult> Cierre()
        {
            var cajaAbierta = await _cajaService.ObtenerTurnoAbiertoAsync();
            if (cajaAbierta == null) return RedirectToAction("Index", "Home");

            var resumen = await _cajaService.ObtenerResumenTurnoAsync(cajaAbierta.IdTurno);
            return View(resumen);
        }

        // PROCESAR EL CIERRE
        [HttpPost]
        public async Task<IActionResult> ProcesarCierre(int IdTurno, string MontoFinalReal)
        {
            try
            {
                decimal valMontoFinalReal = ParsearDecimal(MontoFinalReal);

                await _cajaService.CerrarTurnoAsync(IdTurno, valMontoFinalReal);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                return RedirectToAction("Cierre");
            }
        }

        // GENERAR EL TICKET DE VENTA
        [HttpGet]
        public async Task<IActionResult> Ticket(int id)
        {
            try
            {
                var ticketData = await _ventaService.ObtenerTicketVentaAsync(id);
                return View(ticketData);
            }
            catch (Exception)
            {
                return NotFound("Venta no encontrada");
            }
        }

        // REGISTRAR GASTO O SALIDA EXTRA DE CAJA
        [HttpPost]
        public async Task<IActionResult> RegistrarGasto(string Monto, string Descripcion)
        {
            try
            {
                decimal valMonto = ParsearDecimal(Monto);

                await _cajaService.RegistrarGastoAsync(valMonto, Descripcion);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Home");
            }
        }


        // HISTORIAL DE TURNOS DE CAJA (Auditoría)
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Historial()
        {
            var historial = await _cajaService.ObtenerHistorialTurnosAsync();
            return View(historial);
        }

        // DETALLE DE VENTAS Y YAPE DE UN TURNO ESPECÍFICO
        [HttpGet]
        public async Task<IActionResult> DetalleTurno(int id)
        {
            try
            {
                // Si NO es Administrador, solo le permitimos ver el turno que esté Abierto actualmente
                if (!User.IsInRole("Administrador"))
                {
                    var turnoAbierto = await _cajaService.ObtenerTurnoAbiertoAsync();
                    if (turnoAbierto == null || turnoAbierto.IdTurno != id)
                    {
                        return RedirectToAction("AccesoDenegado", "Auth");
                    }
                }

                ViewBag.Resumen = await _cajaService.ObtenerResumenTurnoAsync(id);
                var ventas = await _cajaService.ObtenerVentasPorTurnoAsync(id);
                return View(ventas);
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Home");
            }
        }

    }
}