using System.ComponentModel.DataAnnotations;

namespace UserManagement.MVC.Models
{
    public class EditUserViewModel
    {
        [Required]
        public string FullName { get; set; }

        public string Email { get; set; } 
        [Required]
        public string Role { get; set; }
    }
}
