using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_DENZELL.Models
{
    public class MovimientoCaja
    {
        [Key]
        public int IdMovimiento { get; set; }

        [Required]
        public int IdTurno { get; set; }

        [Required]
        public string Tipo { get; set; } = string.Empty; // "INGRESO" o "GASTO"

        [Required]
        public string Descripcion { get; set; } = string.Empty; // Ej: "Pago de pasaje", "Fondo para sencillo"

        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdTurno")]
        public TurnoCaja? Turno { get; set; }
    }
}
