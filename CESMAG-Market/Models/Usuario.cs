using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tiendavirtual_Figueroa.Models
{
    public class Usuario
    {
        [Key]
        [Column("user_id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        // CORRECCIÓN: la validación del dominio institucional se hace en el controlador
        // porque [EmailAddress] no soporta restricción de dominio por atributo
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        public string Rol { get; set; } = "estudiante"; // valor por defecto: estudiante

        [Required(ErrorMessage = "El celular es obligatorio")]
        [Range(3000000000, 3999999999, ErrorMessage = "Número de celular no válido (debe empezar por 3)")]
        public long Celular { get; set; }

        [Required(ErrorMessage = "La clave es obligatoria")]
        [MinLength(4, ErrorMessage = "Mínimo 4 caracteres")]
        public string Clave { get; set; } = string.Empty;

        // CORRECCIÓN: campo requerido para filtrar por facultad en el marketplace
        [Required(ErrorMessage = "La facultad es obligatoria")]
        public string Facultad { get; set; } = string.Empty;

        // CORRECCIÓN: calificación del vendedor (promedio de valoraciones recibidas)
        [Range(0.0, 5.0)]
        public double Calificacion { get; set; } = 0.0;

        // CORRECCIÓN: indica si el admin validó el correo institucional del usuario
        public bool Validado { get; set; } = false;
    }
}
