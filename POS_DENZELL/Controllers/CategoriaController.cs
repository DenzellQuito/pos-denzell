using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_DENZELL.Models;
using POS_DENZELL.Services;

namespace POS_DENZELL.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CategoriaController : Controller
    {
        private readonly ICategoriaService _categoriaService;

       
        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categorias = await _categoriaService.ObtenerTodasLasCategoriasAsync();
            return View(categorias);
        }

       
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

       
        [HttpPost]
        public async Task<IActionResult> Crear(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoriaService.CrearCategoriaAsync(categoria);
                    TempData["MensajeExito"] = "Categoría creada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Si el nombre ya existe, el throw del Servicio caerá aquí
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(categoria);
        }

  
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var categoria = await _categoriaService.ObtenerCategoriaPorIdAsync(id);
                return View(categoria);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

       
        [HttpPost]
        public async Task<IActionResult> Editar(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoriaService.ActualizarCategoriaAsync(categoria);
                    TempData["MensajeExito"] = "Categoría actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(categoria);
        }

        // CAMBIAR ESTADO (Soft Delete)
        // Usamos POST por seguridad por ejemplo para que no se puedan desactivar categorías escribiendo la URL
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            try
            {
                await _categoriaService.CambiarEstadoCategoriaAsync(id);
                TempData["MensajeExito"] = "Estado actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
