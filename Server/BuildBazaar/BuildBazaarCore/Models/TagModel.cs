using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class TagModel
    {
        [Key]
        public uint tagID { get; set; }
        public string tagName { get; set; }
        public uint gameID { get; set; }
    }
}