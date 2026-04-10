using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.Models
{
    [Table("Operators")]
    [Index(nameof(Code), IsUnique = true)]
    public class Operators
    {
        [Key]
        [Column("OperatorsId")]
        public int Id { get; set; }
        [Required]
        public int Code { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public int? Number { get; set; }
    }
}

