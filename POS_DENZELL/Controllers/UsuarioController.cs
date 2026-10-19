using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Services;
using System.Security.Claims;

namespace POS_DENZELL.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _usuarioService.ObtenerTodosAsync();
            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(string Nombre, string Username, string Password, string Rol)
        {
            try
            {
                await _usuarioService.CrearUsuarioAsync(Nombre, Username, Password, Rol);
                TempData["Exito"] = "Usuario registrado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int IdUsuario, string Nombre, string Username, string? NuevaPassword, string Rol)
        {
            try
            {
                await _usuarioService.EditarUsuarioAsync(IdUsuario, Nombre, Username, NuevaPassword, Rol);
                TempData["Exito"] = "Datos del usuario actualizados.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            try
            {
                int idUsuarioActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _usuarioService.CambiarEstadoAsync(id, idUsuarioActual);
                TempData["Exito"] = "Estado del usuario actualizado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}