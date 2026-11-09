using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Services;

namespace POS_DENZELL.Controllers
{
    [Authorize] // Tanto Cajero como Admin pueden buscar y crear clientes desde el POS
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        // 1. Pantalla de Directorio de Clientes
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientes = await _clienteService.ObtenerTodosConHistorialAsync();
            return View(clientes);
        }

        // 2. Endpoint AJAX para buscar clientes en vivo dentro del Modal de Cobro
        [HttpGet]
        public async Task<IActionResult> BuscarPos(string termino)
        {
            var resultados = await _clienteService.BuscarClientesPosAsync(termino);
            return Json(resultados);
        }

        // 3. Endpoint AJAX para registrar un cliente nuevo sin salir del Modal de Cobro
        [HttpPost]
        public async Task<IActionResult> CrearRapidoPos(string tipoDoc, string numeroDoc, string nombre, string? telefono, string? direccion)
        {
            try
            {
                var nuevo = await _clienteService.CrearClienteAsync(tipoDoc, numeroDoc, nombre, telefono, direccion);
                return Json(new
                {
                    success = true,
                    idCliente = nuevo.IdCliente,
                    documento = $"{nuevo.TipoDocumento}: {nuevo.NumeroDocumento}",
                    nombre = nuevo.NombreCompleto
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        // 4. Acciones del CRUD administrativo (Crear, Editar, CambiarEstado)
        [HttpPost]
        public async Task<IActionResult> Crear(string TipoDocumento, string NumeroDocumento, string NombreCompleto, string? Telefono, string? Direccion)
        {
            try
            {
                await _clienteService.CrearClienteAsync(TipoDocumento, NumeroDocumento, NombreCompleto, Telefono, Direccion);
                TempData["Exito"] = "Cliente registrado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int IdCliente, string TipoDocumento, string NumeroDocumento, string NombreCompleto, string? Telefono, string? Direccion)
        {
            try
            {
                await _clienteService.EditarClienteAsync(IdCliente, TipoDocumento, NumeroDocumento, NombreCompleto, Telefono, Direccion);
                TempData["Exito"] = "Datos del cliente actualizados.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            try
            {
                await _clienteService.CambiarEstadoAsync(id);
                TempData["Exito"] = "Estado del cliente actualizado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}