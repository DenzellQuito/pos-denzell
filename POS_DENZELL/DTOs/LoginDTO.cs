using System.ComponentModel.DataAnnotations;

namespace POS_DENZELL.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; } = string.Empty;
    }
}
