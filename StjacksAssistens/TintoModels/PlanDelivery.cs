using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.TintoModels
{
    [Table("PlanDelivery")]
    public class PlanDelivery
    {
        [Key]
        public int PlanDeliveryId { get; set; }
        public string LoteCode { get; set; } = null!;
        public DateTime? DeliveryDate { get; set; }
        public string PrintColoJumb { get; set; } = null!;
        public decimal SumKl { get; set; }
        public int SumRolls { get; set; }
        public string Status { get; set; } = null!;
        public string Comments { get; set; } = null!;

    }
}
