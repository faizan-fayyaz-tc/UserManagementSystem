using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using UserManagement.API.Exceptions;
using UserManagement.API.Models;

namespace UserManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.ProfilePicturePath,
                    Roles = roles 
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                throw new NotFoundException("User not found");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != user.Id && !isAdmin)
                throw new UnauthorizedAccessException("You can only view your own profile");

            var roles = await _userManager.GetRolesAsync(user); 

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.ProfilePicturePath,
                Roles = roles 
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                throw new NotFoundException("User not found");

            user.FullName = model.FullName;

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if(!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception("Update failed: " + errors);
            }

            return Ok("User Updated Successfully");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("User not found");

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception("Deletion failed: " + errors);
            }

            return Ok("User deleted");
        }

        [HttpPost("{id}/upload-profile")]
        public async Task<IActionResult> UploadProfilePicture(string id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No file uploaded");

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                throw new NotFoundException("User not found");

            // Generate unique filename
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save path to DB
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            user.ProfilePicturePath = $"{baseUrl}/uploads/{fileName}";

            await _userManager.UpdateAsync(user);

            return Ok("Profile picture uploaded successfully");
        }

        [HttpGet("test-exception")]
        public IActionResult TestException()
        {
            throw new Exception("This is a test exception from controller");
        }

    }
}
