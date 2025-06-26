using Amazon.Runtime;
using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class BuildModel
    {
        [Key]
        public uint buildID { get; set; }
        public uint userID { get; set; }
        public uint imageID { get; set; }
        public string buildName { get; set; }
        public string filePath { get; set; }
        public bool isPublic {get; set;}
        public uint gameID { get; set; }
        public uint classID { get; set; }
        public string tags { get; set; }
    }
}