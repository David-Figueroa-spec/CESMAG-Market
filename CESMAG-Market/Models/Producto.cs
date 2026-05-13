using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Tiendavirtual_Figueroa.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        // CORRECCIÓN: cambiado de double a decimal para valores monetarios (evita errores de redondeo)
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0, 9999999, ErrorMessage = "El precio debe estar entre 0 y 9,999,999")]
        public decimal Precio { get; set; }

        // CORRECCIÓN: eliminado Stock (en un marketplace P2P cada publicación es una unidad, 
        // el estado se maneja con el campo Estado)

        // Estado del producto: "Disponible" o "Vendido"
        public string Estado { get; set; } = "Disponible";

        public int CategoriaId { get; set; }

        [ValidateNever]
        public virtual Categoria Categoria { get; set; } = null!;

        // CORRECCIÓN: FK al vendedor (estudiante que publica el producto)
        public int UsuarioId { get; set; }

        [ValidateNever]
        public virtual Usuario Usuario { get; set; } = null!;

        public string ImagenUrl { get; set; } = string.Empty;
    }
}
