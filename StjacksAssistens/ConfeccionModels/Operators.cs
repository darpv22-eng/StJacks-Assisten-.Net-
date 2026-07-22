using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.ConfeccionModels
{
    [Table("Operators")]
    public class Operators
    {
        [Key]
        public int OperatorsId { get; set; }

        // SOLUCCIÓN CRÍTICA: Quitamos el [Required] para que EF Core permita los nulos de la BD
        public int? CategoryId { get; set; }

        [Required]
        public int Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        // Mapeo de las nuevas columnas de alteración (Correctamente Nullables)
        public int? AreaId { get; set; }
        public int? LineaId { get; set; }

        // Propiedades de Navegación Virtuales para EF Core
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("AreaId")]
        public virtual Category? Area { get; set; }

        [ForeignKey("LineaId")]
        public virtual Category? Linea { get; set; }
    }
}