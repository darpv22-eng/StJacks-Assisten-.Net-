using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.TintoModels
{
    public class OperatorsTinto
    {
        [Key]
        public int OperatorsId { get; set; }
        public int? GroupsId { get; set; }
        public int Code { get; set; }
        public string Names { get; set; } = null!;

        [ForeignKey("GroupsId")]
        public virtual Groups? Group { get; set; }

    }
}
