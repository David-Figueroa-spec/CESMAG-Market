using System.ComponentModel.DataAnnotations;

namespace Tiendavirtual_Figueroa.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string Descripcion { get; set; } = string.Empty;


        public string Estado { get; set; } = "Activo";
    }
}