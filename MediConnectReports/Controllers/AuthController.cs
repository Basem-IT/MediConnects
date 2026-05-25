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

        // show login page
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // handle login form submission
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // try to get jwt token from api
            var token = await _apiService.LoginAsync(model);

            // login fails show the error message
            if (token == null)
            {
                model.ErrorMessage = "Invalid login or you are not a Clinic Manager.";
                return View(model);
            }

            // save token in session after successful login
            HttpContext.Session.SetString("JwtToken", token);

            return RedirectToAction("Dashboard", "Reports");
        }

        // logout user and clear session
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JwtToken");
            return RedirectToAction("Login");
        }
    }
}