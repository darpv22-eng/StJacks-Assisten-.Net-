using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StjacksAssistens.Models
{

    [Table("Category")]
    [Index(nameof(Name), IsUnique = true)]
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [ForeignKey("Operators")]
        public int OperatorsId { get; set; }

        [NotMapped]
        public Operators Operators { get; set; } = null!;
    }
}
