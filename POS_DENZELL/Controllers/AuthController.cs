using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.DTOs;
using POS_DENZELL.Services;
using System.Security.Claims;

namespace POS_DENZELL.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        // Inyectamos el servicio de Autenticación
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");

            return View();
        }

        // Verificacion login
        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            // El servicio valida usuario, contraseña y que esté Activo
            var usuarioEncontrado = await _authService.ValidarCredencialesAsync(modelo.Username, modelo.Password);

            if (usuarioEncontrado == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View(modelo);
            }

            // LAS COOKIES 
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioEncontrado.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuarioEncontrado.Nombre),
                new Claim(ClaimTypes.Role, usuarioEncontrado.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        // Botón de Salir
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // Pantalla cuando un Cajero intenta entrar a zona de Administrador
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}