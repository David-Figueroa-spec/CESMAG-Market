using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Tiendavirtual_Figueroa.Models
{
    // CORRECCIÓN: modelo nuevo requerido por el dominio P2P
    // Registra la publicación de un producto en el marketplace
    public class Anuncio
    {
        public int Id { get; set; }

        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        // Estado de la publicación: "Disponible" o "Vendido"
        public string Estado { get; set; } = "Disponible";

        // FK al producto publicado
        public int ProductoId { get; set; }

        [ValidateNever]
        public virtual Producto Producto { get; set; } = null!;

        // FK al vendedor (quien publica)
        public int VendedorId { get; set; }

        [ValidateNever]
        public virtual Usuario Vendedor { get; set; } = null!;
    }
}
