using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class ImageModel
    {
        [Key]
        public uint imageID { get; set; }
        public uint buildID { get; set; }
        public uint typeID { get; set; }
        public uint userID { get; set; }
        public bool isPublic { get; set; }
        public string filePath { get; set; }
        public int imageOrder { get; set; }
    }
}