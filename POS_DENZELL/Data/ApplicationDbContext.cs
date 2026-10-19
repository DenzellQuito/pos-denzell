using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Models;

namespace POS_DENZELL.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

      
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }
        public DbSet<TurnoCaja> TurnosCaja { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ejemplo: Asegurar que el Username sea único a nivel de base de datos
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configuración estricta de la relación Venta -> VentaDetalle (Evita borrados accidentales en cascada)
            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.VentaCabecera)
                .WithMany(v => v.Detalles)
                .HasForeignKey(vd => vd.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            // Prohibir borrar un Turno si tiene Ventas
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Turno)
                .WithMany(t => t.VentasDelTurno)
                .HasForeignKey(v => v.IdTurno)
                .OnDelete(DeleteBehavior.Restrict);

            // Prohibir borrar un Usuario si tiene Ventas
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.UsuarioCajero)
                .WithMany()
                .HasForeignKey(v => v.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            //Prohibir borrar un Usuario si abrió un Turno de Caja
            modelBuilder.Entity<TurnoCaja>()
                .HasOne(t => t.UsuarioCajero)
                .WithMany()
                .HasForeignKey(t => t.IdUsuarioApertura)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
