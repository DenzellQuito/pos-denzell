using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_DENZELL.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio")]
        [StringLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción del producto es obligatoria")]
        [StringLength(150)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Precio { get; set; }

        public int Stock { get; set; } = 0;
        public string? RutaImagen { get; set; }
        public bool Activo { get; set; } = true;

        // --- RELACIÓN CON CATEGORÍA ---
        public int? IdCategoria { get; set; } // El signo ? significa que puede ser nulo (productos sin categoría)

        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }

        public bool EsFiesta { get; set; } = false;
    }
}