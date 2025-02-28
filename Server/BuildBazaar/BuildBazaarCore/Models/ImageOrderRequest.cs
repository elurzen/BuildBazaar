using BuildBazaarCore.Models;
using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class ImageOrderRequest
    {
        public List<ImageOrder> newOrder { get; set; }
        public int buildID { get; set; }
    }
}
