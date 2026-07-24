using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.TintoModels
{
    public class OperatorsTintos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OperatorsTintosId { get; set; }
        public int? GroupsId { get; set; }
        public int Codes { get; set; }
        public string Names { get; set; } = null!;

        [ForeignKey("GroupsId")]
        public virtual Groups? Group { get; set; }

    }
}
