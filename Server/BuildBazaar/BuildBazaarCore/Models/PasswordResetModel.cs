using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class PasswordResetModel
    {
        [Key]
        public uint passwordResetID { get; set; }
        public uint userID { get; set; }
        public string resetToken { get; set; }
        public DateTime expiration { get; set; }
    }
}
