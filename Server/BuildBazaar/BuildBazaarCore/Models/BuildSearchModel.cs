using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class BuildSearchModel
    {
        [Key]
        public uint buildID { get; set; }
        public string buildName { get; set; }
        public uint userID { get; set; }
        public string userName { get; set; }
        public uint imageID { get; set; }        
        public string filePath { get; set; }
        public bool isPublic {get; set;}
        public string gameName { get; set; }
        public string className { get; set; }
        //public string List<string> tags  { get; set; }
    }
}