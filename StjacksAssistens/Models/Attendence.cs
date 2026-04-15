using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.Models
{
    [Table("Attendence")]
    public class Attendence
    {
        [Key]
        [Column("AttendeceId")]
        public int Id { get; set; }
        [Column("PeriodId")]
        public int PeriodId { get; set; }
        [Column("OperatorsId")]
        public int OperatorsId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } // X, PP, INC, ISSS

        [ForeignKey("PeriodId")]
        public virtual Periodss? Period { get; set; }
    }
}
