using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class BuildUrlModel
    {
        [Key]
        public uint buildUrlID { get; set; }
        public uint buildID { get; set; }
        public string buildUrlName { get; set; }
        public string buildUrl { get; set; }

    }
}