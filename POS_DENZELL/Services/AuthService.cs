using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<Usuario> _hasher;

        public AuthService(ApplicationDbContext context, IPasswordHasher<Usuario> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<Usuario?> ValidarCredencialesAsync(string username, string password)
        {
            // 1) Buscamos solo por usuario y estado (Regla de Borrado Lógico).
            //    La contraseña ya no se compara en SQL: se verifica contra su hash.
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == username && u.Activo == true);

            if (usuario == null) return null;

            // 2) Verificación del hash (con sal incluida en el propio hash)
            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);

            if (resultado == PasswordVerificationResult.Failed) return null;

            // 3) Si el algoritmo se actualiza en el futuro, re-hasheamos de forma transparente
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = _hasher.HashPassword(usuario, password);
                await _context.SaveChangesAsync();
            }

            return usuario;
        }
    }
}