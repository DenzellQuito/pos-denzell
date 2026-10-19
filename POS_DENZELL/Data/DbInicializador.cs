using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Models;

namespace POS_DENZELL.Data
{
    public static class DbInicializador
    {
        // Los hashes de PasswordHasher (formato v3) siempre empiezan con este prefijo en Base64
        private const string PrefijoHash = "AQAAAA";

        public static async Task EjecutarAsync(
            ApplicationDbContext context,
            IPasswordHasher<Usuario> hasher,
            IConfiguration config,
            IHostEnvironment env)
        {
            // Aplica las migraciones pendientes (equivale a "dotnet ef database update")
            await context.Database.MigrateAsync();

            // 1) Migración única: convierte contraseñas heredadas en texto plano a hash
            var heredados = await context.Usuarios
                .Where(u => !u.PasswordHash.StartsWith(PrefijoHash))
                .ToListAsync();

            foreach (var u in heredados)
                u.PasswordHash = hasher.HashPassword(u, u.PasswordHash);

            if (heredados.Count > 0)
                await context.SaveChangesAsync();

            // 2) Seed: si la base está vacía, crea el administrador inicial
            if (await context.Usuarios.AnyAsync()) return;

            // En Development usa una clave de prueba; en producción exige configurarla
            // (appsettings, variable de entorno AdminInicial__Password o user-secrets)
            var password = config["AdminInicial:Password"]
                           ?? (env.IsDevelopment() ? "Admin123!" : null);

            if (password == null) return;

            var admin = new Usuario
            {
                Nombre = "Administrador",
                Username = "admin",
                Rol = "Administrador",
                Activo = true
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}