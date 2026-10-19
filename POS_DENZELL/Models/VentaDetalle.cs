using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_DENZELL.Models
{
    public class VentaDetalle
    {
        [Key]
        public int IdDetalle { get; set; }

        [Required]
        public int IdVenta { get; set; }

        [Required]
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal SubTotal { get; set; }

        // --- PROPIEDADES DE NAVEGACIÓN ---
        [ForeignKey("IdVenta")]
        public Venta? VentaCabecera { get; set; }

        [ForeignKey("IdProducto")]
        public Producto? ProductoVendido { get; set; }
    }
}
