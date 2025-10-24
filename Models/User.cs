using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POE___PART_2___Contract_Monthly_Claims_System.Models
{
    public class User
    {
        public User()
        {
            Claims = new List<Claim>();
        }

        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public virtual ICollection<Claim> Claims { get; set; }
    }
}