using Microsoft.EntityFrameworkCore;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Data
{
    public class TiendaContext : DbContext
    {
        public TiendaContext(DbContextOptions<TiendaContext> options)
            : base(options)
        {
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        // CORRECCIÓN: DbSet nuevo requerido para el modelo Anuncio
        public DbSet<Anuncio> Anuncios { get; set; }

        // CORRECCIÓN: CarritoItem NO va aquí (era una entidad de sesión, no de base de datos)
        // El carrito de compras tampoco aplica en un marketplace P2P de contacto directo

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CORRECCIÓN: especificar precisión del decimal para evitar truncamiento silencioso
            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2); // hasta 9,999,999,999,999,999.99

            // Evita borrado en cascada múltiple (SQL Server no lo permite sin configuración explícita)
            // Producto → Usuario: si se elimina un usuario sus productos quedan sin vendedor
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Anuncio → Producto
            modelBuilder.Entity<Anuncio>()
                .HasOne(a => a.Producto)
                .WithMany()
                .HasForeignKey(a => a.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Anuncio → Vendedor: restricción para evitar cascada múltiple
            modelBuilder.Entity<Anuncio>()
                .HasOne(a => a.Vendedor)
                .WithMany()
                .HasForeignKey(a => a.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
