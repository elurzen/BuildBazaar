using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class ImageOrder
    {
        public uint imageID { get; set; }
        public int imageOrder { get; set; }
    }
}