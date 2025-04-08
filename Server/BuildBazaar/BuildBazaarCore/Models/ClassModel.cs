using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class ClassModel
    {
        [Key]
        public uint classID { get; set; }
        public string className { get; set; }
        public uint gameID { get; set; }
    }
}