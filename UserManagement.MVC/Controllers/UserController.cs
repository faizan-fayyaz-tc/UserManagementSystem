using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UserManagement.MVC.Models;


namespace UserManagement.MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> All()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("users");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Failed to load users.";
                return View(new List<UserViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(users);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

       
        [HttpPost]
        public async Task<IActionResult> Add(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("auth/register", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("All");

            ViewBag.Error = "Failed to create user.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"users/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "User not found.";
                return RedirectToAction("All");
            }

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var model = new RegisterViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Roles != null ? user.Roles.FirstOrDefault() : ""
            };

            ViewBag.UserId = id;
            ViewBag.UserName = user.FullName;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.UserId = id;
                ViewBag.UserName = model.FullName;
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateDto = new
            {
                FullName = model.FullName,
                Role = model.Role
            };

            var json = JsonSerializer.Serialize(updateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"users/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "✅ User updated successfully.";
                return RedirectToAction("All"); // Redirect prevents form resubmission
            }

            ViewBag.UserId = id;
            ViewBag.UserName = model.FullName;
            ViewBag.Error = "Update failed.";
            return View(model); // This line causes browser to warn on refresh
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"users/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction("All");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Failed to load user profile.";
                return View(new UserViewModel());  
            }

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(user); 
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid image.";
                return RedirectToAction("Profile");
            }

            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);

            var response = await client.PostAsync($"users/{userId}/upload-profile", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = " Profile picture uploaded successfully.";
            }
            else
            {
                TempData["Error"] = " Upload failed. Please try again.";
            }

            return RedirectToAction("Profile");
        }


    }
}
