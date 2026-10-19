using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_DENZELL.Models
{
    public class Venta
    {
        [Key]
        public int IdVenta { get; set; }

        [Required]
        public int IdTurno { get; set; } // Vinculado a la caja

        [Required]
        public int IdUsuario { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string TipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Correlativo { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PagoEfectivo { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PagoYape { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Vuelto { get; set; }

        public string? NumeroOperacion { get; set; }

        public string? RutaFotoComprobante { get; set; }
        public string EstadoSunat { get; set; } = string.Empty;

        [ForeignKey("IdTurno")]
        public TurnoCaja? Turno { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? UsuarioCajero { get; set; }

        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();

        
        public string? ClienteDocumento { get; set; } 
        public string? ClienteNombre { get; set; } 

        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalIGV { get; set; } 

        
        public string? MensajeSunat { get; set; } // Para guardar errores 
    }
}