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

        // Show form
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // Handle form submission
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

        // GET: /User/Edit/abc123
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
            return View(model);
        }

        // POST: /User/Edit/abc123
        [HttpPost]
        public async Task<IActionResult> Edit(string id, RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("API");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"users/{id}", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("All");

            ViewBag.Error = "Update failed.";
            return View(model);
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



    }
}
