using System.ComponentModel.DataAnnotations;

namespace POS_DENZELL.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
