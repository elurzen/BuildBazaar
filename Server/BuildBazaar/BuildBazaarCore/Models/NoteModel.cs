using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class NoteModel
    {
        [Key]
        public uint noteID { get; set; }
        public uint buildID { get; set; }
        public uint userID { get; set; }
        public bool isPublic { get; set; }
        public string filePath { get; set; }
    }
}