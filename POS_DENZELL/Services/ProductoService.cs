using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public interface IProductoService
    {
        Task<List<Producto>> ObtenerTodosAsync();
        Task<List<Categoria>> ObtenerCategoriasActivasAsync(); 
        Task CrearProductoAsync(Producto producto, IFormFile? foto); 
        // ... (Debajo de los métodos que ya tenías)
        Task<Producto?> ObtenerPorIdAsync(int id);
        Task ActualizarProductoAsync(Producto productoModificado, IFormFile? nuevaFoto);
        // Agrega esta nueva línea a tu interfaz
        Task<IEnumerable<Producto>> ObtenerProductosFiltradosAsync(string? buscar, int? idCategoria);
        //lleva el stock critico
        Task<List<Producto>> ObtenerStockCriticoAsync(int limite = 5, int cantidad = 10);
    }

    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Inyectamos el contexto de BD y el entorno web (para guardar la foto)
        public ProductoService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<List<Producto>> ObtenerTodosAsync()
        {
            return await _context.Productos
                                 .Include(p => p.Categoria)
                                 .Where(p => p.Activo)
                                 .OrderBy(p => p.Descripcion)
                                 .ToListAsync();
        }

        // Obtiene las categorías para el desplegable del formulario
        public async Task<List<Categoria>> ObtenerCategoriasActivasAsync()
        {
            return await _context.Categorias
                                 .Where(c => c.Activo)
                                 .OrderBy(c => c.Nombre)
                                 .ToListAsync();
        }

        // Lógica de guardado y manejo de archivos
        public async Task CrearProductoAsync(Producto producto, IFormFile? foto)
        {
            // Evitar códigos de barras duplicados en productos activos ---
            bool codigoExiste = await _context.Productos
                .AnyAsync(p => p.CodigoBarras == producto.CodigoBarras && p.Activo == true);

            if (codigoExiste)
            {
                throw new Exception($"Ya existe un producto activo con el código de barras '{producto.CodigoBarras}'.");
            }

            // Manejo de la imagen (Si el usuario subió una)
            if (foto != null && foto.Length > 0)
            {
                var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!extensionesPermitidas.Contains(extension))
                {
                    throw new Exception("Formato no permitido. Solo se aceptan imágenes JPG, PNG o WEBP.");
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "productos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                //formateo de nombre archivo
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"PROD_{producto.CodigoBarras}_{timestamp}{extension}";
                

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(fileStream);
                }

                producto.RutaImagen = "/images/productos/" + uniqueFileName;
            }

           
            producto.Activo = true;
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
        }

        // Trae un solo producto por su ID para llenar el formulario
        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos.FindAsync(id);
        }

        public async Task<IEnumerable<Producto>> ObtenerProductosFiltradosAsync(string? buscar, int? idCategoria)
        {
            // Empezamos trayendo solo los activos y adjuntando su categoría
            var query = _context.Productos.Include(p => p.Categoria).Where(p => p.Activo == true).AsQueryable();

            // Filtramos por texto (Nombre o Código) si el usuario escribió algo
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Descripcion.Contains(buscar) || p.CodigoBarras.Contains(buscar));
            }

            // Filtramos por categoría si el usuario seleccionó una
            if (idCategoria.HasValue)
            {
                query = query.Where(p => p.IdCategoria == idCategoria.Value);
            }

            return await query.ToListAsync();
        }

        // Actualiza los datos y maneja el reemplazo de la foto
        public async Task ActualizarProductoAsync(Producto productoModificado, IFormFile? nuevaFoto)
        {
            // Evitar códigos duplicados
            bool codigoExiste = await _context.Productos
                .AnyAsync(p => p.CodigoBarras == productoModificado.CodigoBarras
                            && p.Activo == true
                            && p.IdProducto != productoModificado.IdProducto);

            if (codigoExiste)
            {
                throw new Exception($"Ya existe otro producto activo usando el código '{productoModificado.CodigoBarras}'.");
            }

            var productoExistente = await _context.Productos.FindAsync(productoModificado.IdProducto);
            if (productoExistente == null) throw new Exception("Producto no encontrado.");

            productoExistente.CodigoBarras = productoModificado.CodigoBarras;
            productoExistente.Descripcion = productoModificado.Descripcion;
            productoExistente.IdCategoria = productoModificado.IdCategoria;
            productoExistente.Precio = productoModificado.Precio;
            productoExistente.Stock = productoModificado.Stock;
            productoExistente.EsFiesta = productoModificado.EsFiesta;

            // Si el usuario subió una NUEVA foto, la procesamos
            if (nuevaFoto != null && nuevaFoto.Length > 0)
            {
                var extension = Path.GetExtension(nuevaFoto.FileName).ToLowerInvariant();
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!extensionesPermitidas.Contains(extension))
                {
                    throw new Exception("Formato no permitido. Solo se aceptan imágenes JPG, PNG o WEBP.");
                }

                //
                if (!string.IsNullOrEmpty(productoExistente.RutaImagen))
                {
                    string nombreFotoAntigua = Path.GetFileName(productoExistente.RutaImagen);
                    string rutaFisicaAntigua = Path.Combine(_env.WebRootPath, "images", "productos", nombreFotoAntigua);

                    
                    if (System.IO.File.Exists(rutaFisicaAntigua))
                    {
                        System.IO.File.Delete(rutaFisicaAntigua);
                    }
                }
                

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "productos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Nomenclatura profesional para la foto actualizada 
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"PROD_{productoModificado.CodigoBarras}_{timestamp}{extension}";
                

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await nuevaFoto.CopyToAsync(fileStream);
                }

                productoExistente.RutaImagen = "/images/productos/" + uniqueFileName;
            }

            _context.Productos.Update(productoExistente);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Producto>> ObtenerStockCriticoAsync(int limite = 5, int cantidad = 10)
        {
            return await _context.Productos
                .Where(p => p.Stock <= limite)
                .OrderBy(p => p.Stock)
                .Take(cantidad)
                .ToListAsync();
        }
    }
}