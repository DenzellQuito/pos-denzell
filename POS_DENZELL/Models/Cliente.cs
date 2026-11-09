using System.ComponentModel.DataAnnotations;

namespace POS_DENZELL.Models
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required]
        [StringLength(15)]
        public string TipoDocumento { get; set; } = "DNI"; // DNI, RUC, CE

        [Required]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        public ICollection<Venta> Compras { get; set; } = new List<Venta>();
    }
}