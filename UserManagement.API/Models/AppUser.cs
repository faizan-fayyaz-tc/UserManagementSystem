using Microsoft.AspNetCore.Identity;

namespace UserManagement.API.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfilePicturePath { get; set; }
    }
}
