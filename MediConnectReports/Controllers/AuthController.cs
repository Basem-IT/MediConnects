using MediConnectReports.Models;
using MediConnectReports.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediConnectReports.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var token = await _apiService.LoginAsync(model);

            if (token == null)
            {
                model.ErrorMessage = "Invalid login or you are not a Clinic Manager.";
                return View(model);
            }

            HttpContext.Session.SetString("JwtToken", token);

            return RedirectToAction("Dashboard", "Reports");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JwtToken");
            return RedirectToAction("Login");
        }
    }
}