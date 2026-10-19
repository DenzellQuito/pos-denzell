using System.ComponentModel.DataAnnotations;

namespace POS_DENZELL.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty; // Ej: "Chocolates", "Galletas"

        public bool Activo { get; set; } = true;

        // Relación: Una categoría puede tener muchos productos
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}