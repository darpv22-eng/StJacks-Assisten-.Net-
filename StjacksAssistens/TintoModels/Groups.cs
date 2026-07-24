using System.ComponentModel.DataAnnotations.Schema;

namespace StjacksAssistens.TintoModels
{
   [Table("Groups")]
    public class Groups
    {

        public int GroupsId { get; set; }
        public string Names { get; set; }

    }
}
