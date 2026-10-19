using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public interface IAuthService
    {
        Task<Usuario?> ValidarCredencialesAsync(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ValidarCredencialesAsync(string username, string password)
        {
            // Buscamos al usuario y validamos de una vez que esté Activo (Regla de Borrado Lógico)
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == username
                                       && u.PasswordHash == password
                                       && u.Activo == true);

            return usuario;
        }
    }
}