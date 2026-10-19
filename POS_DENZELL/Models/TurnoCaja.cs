using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_DENZELL.Models
{
    public class TurnoCaja
    {
        [Key]
        public int IdTurno { get; set; }

        [Required]
        public int IdUsuarioApertura { get; set; }

        public DateTime FechaHoraApertura { get; set; } = DateTime.Now;
        public DateTime? FechaHoraCierre { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoInicial { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MontoFinalCalculado { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MontoFinalReal { get; set; }

        [Required]
        public string Estado { get; set; } = "Abierto";

        [ForeignKey("IdUsuarioApertura")]
        public Usuario? UsuarioCajero { get; set; }

        // --- RELACIONES CON LAS OTRAS TABLAS ---
        public ICollection<Venta> VentasDelTurno { get; set; } = new List<Venta>();
        public ICollection<MovimientoCaja> MovimientosExtras { get; set; } = new List<MovimientoCaja>();
    }
}