using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public interface ICategoriaService
    {
        // Traer todas las categorías (incluso las desactivadas, para el CRUD)
        Task<IEnumerable<Categoria>> ObtenerTodasLasCategoriasAsync();

        // Traer solo las activas (para llenar el select al crear un Producto)
        Task<IEnumerable<Categoria>> ObtenerCategoriasActivasAsync();

        // Buscar una categoría por su ID
        Task<Categoria> ObtenerCategoriaPorIdAsync(int id);

        // Crear una nueva
        Task CrearCategoriaAsync(Categoria categoria);

        // Editar el nombre
        Task ActualizarCategoriaAsync(Categoria categoria);

        // Soft Delete (Activar/Desactivar)
        Task CambiarEstadoCategoriaAsync(int id);
    }
    public class CategoriaService : ICategoriaService
    {
        private readonly ApplicationDbContext _context;

        public CategoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Categoria>> ObtenerTodasLasCategoriasAsync()
        {
            return await _context.Categorias
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<IEnumerable<Categoria>> ObtenerCategoriasActivasAsync()
        {
            return await _context.Categorias
                .Where(c => c.Activo == true)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<Categoria> ObtenerCategoriaPorIdAsync(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) throw new Exception("Categoría no encontrada.");
            return categoria;
        }

        public async Task CrearCategoriaAsync(Categoria categoria)
        {
            string nombreLimpio = categoria.Nombre.Trim();

            bool nombreExiste = await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombreLimpio.ToLower());

            if (nombreExiste)
            {
                throw new Exception($"La categoría '{nombreLimpio}' ya existe.");
            }

            categoria.Nombre = nombreLimpio;
            categoria.Activo = true;
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarCategoriaAsync(Categoria categoriaModificada)
        {
            var categoriaExistente = await ObtenerCategoriaPorIdAsync(categoriaModificada.IdCategoria);
            string nombreLimpio = categoriaModificada.Nombre.Trim();

            bool nombreExiste = await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombreLimpio.ToLower()
                            && c.IdCategoria != categoriaModificada.IdCategoria);

            if (nombreExiste)
            {
                throw new Exception($"El nombre '{nombreLimpio}' ya está siendo usado.");
            }

            categoriaExistente.Nombre = nombreLimpio;
            _context.Categorias.Update(categoriaExistente);
            await _context.SaveChangesAsync();
        }

        public async Task CambiarEstadoCategoriaAsync(int id)
        {
            var categoria = await ObtenerCategoriaPorIdAsync(id);
            categoria.Activo = !categoria.Activo;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
        }
    }
}
