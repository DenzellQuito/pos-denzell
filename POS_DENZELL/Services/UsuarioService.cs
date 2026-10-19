using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObtenerTodosAsync();
        Task CrearUsuarioAsync(string nombre, string username, string password, string rol);
        Task EditarUsuarioAsync(int idUsuario, string nombre, string username, string? nuevaPassword, string rol);
        Task CambiarEstadoAsync(int idUsuario, int idUsuarioActual);
    }

    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;

        public UsuarioService(ApplicationDbContext context)
        {
            _context = context;
        }

      
        public async Task<List<Usuario>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .OrderByDescending(u => u.Activo)
                .ThenBy(u => u.Rol)
                .ThenBy(u => u.Nombre)
                .ToListAsync();
        }

   
        public async Task CrearUsuarioAsync(string nombre, string username, string password, string rol)
        {
            string userLimpio = username.Trim();

            bool existe = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == userLimpio.ToLower());

            if (existe)
                throw new Exception($"El nombre de usuario '{userLimpio}' ya está en uso.");

            var nuevoUsuario = new Usuario
            {
                Nombre = nombre.Trim(),
                Username = userLimpio,
                PasswordHash = password.Trim(),
                Rol = rol == "Administrador" ? "Administrador" : "Cajero",
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
        }

       
        public async Task EditarUsuarioAsync(int idUsuario, string nombre, string username, string? nuevaPassword, string rol)
        {
            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            string userLimpio = username.Trim();

            bool duplicado = await _context.Usuarios
                .AnyAsync(u => u.IdUsuario != idUsuario && u.Username.ToLower() == userLimpio.ToLower());

            if (duplicado)
                throw new Exception($"El nombre de usuario '{userLimpio}' ya pertenece a otra cuenta.");

            usuario.Nombre = nombre.Trim();
            usuario.Username = userLimpio;
            usuario.Rol = rol == "Administrador" ? "Administrador" : "Cajero";

            if (!string.IsNullOrWhiteSpace(nuevaPassword))
            {
                usuario.PasswordHash = nuevaPassword.Trim();
            }

            await _context.SaveChangesAsync();
        }

        // Activar / Desactivar usuario
        public async Task CambiarEstadoAsync(int idUsuario, int idUsuarioActual)
        {
            if (idUsuario == idUsuarioActual)
                throw new Exception("Por seguridad, no puedes desactivar tu propia cuenta en sesión.");

            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();
        }
    }
}