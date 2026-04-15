using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.Models
{
    public class Periodss
    {
        [Key]
        [Column("PeriodId")]
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Attendence> Attendances { get; set; }
    }
}
