using System.ComponentModel.DataAnnotations;

namespace BuildBazaarCore.Models
{
    public class UserModel
    {
        [Key]
        public uint userID { get; set; }

        [Required(ErrorMessage = "The username field is required.")]
        [MaxLength(50)]
        public string username { get; set; }

        [Required(ErrorMessage = "The email field is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(100)]
        public string email { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        [MinLength(6, ErrorMessage = "The password must be at least 6 characters.")]
        [MaxLength(50)]
        public string password { get; set; }
    }
}