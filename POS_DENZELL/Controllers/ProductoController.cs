using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using POS_DENZELL.Models;
using POS_DENZELL.Services;

namespace POS_DENZELL.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProductoController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        
        public ProductoController(IProductoService productoService, ICategoriaService categoriaService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        // Pantalla Principal: Lista de todos los dulces
        [HttpGet]
        public async Task<IActionResult> Index(string? buscar, int? idCategoria)
        {
            // Guardamos el texto buscado para que la cajita no se borre al recargar
            ViewData["FiltroBusqueda"] = buscar;

            // Traemos las categorías activas desde su propio servicio
            var categorias = await _categoriaService.ObtenerCategoriasActivasAsync();
            
            ViewBag.ListaCategorias = new SelectList(categorias, "IdCategoria", "Nombre", idCategoria);

            var productos = await _productoService.ObtenerProductosFiltradosAsync(buscar, idCategoria);

            return View(productos);
        }

        // PANTALLA DE CREACIÓN (GET)
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            // Enviamos la lista de categorías a la vista para armar el <select>
            ViewBag.Categorias = await _productoService.ObtenerCategoriasActivasAsync();
            return View();
        }

        // RECIBIR LOS DATOS Y GUARDAR (POST)
        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto, IFormFile? Foto)
        {
            try
            {
                // Enviamos todo al servicio
                await _productoService.CrearProductoAsync(producto, Foto);

                // Si todo sale bien, lo devolvemos a la tabla de inventario
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear el producto: " + ex.Message);
                ViewBag.Categorias = await _productoService.ObtenerCategoriasActivasAsync();
                return View(producto);
            }
        }

 
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);
            if (producto == null) return RedirectToAction(nameof(Index));

            // Cargamos las categorías para que el <select> funcione
            ViewBag.Categorias = await _productoService.ObtenerCategoriasActivasAsync();

            // Le mandamos el producto encontrado a la vista para que pre-llene los campos
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Producto producto, IFormFile? Foto)
        {
            try
            {
                // El servicio se encarga de la lógica inteligente de reemplazar o mantener la foto
                await _productoService.ActualizarProductoAsync(producto, Foto);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                ViewBag.Categorias = await _productoService.ObtenerCategoriasActivasAsync();
                return View(producto);
            }
        }
    }
}