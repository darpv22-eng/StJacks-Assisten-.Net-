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
        [Column("CategoryId")]
        public int Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
