using MediConnectAPI.Data;
using MediConnectAPI.Models;
using MediConnectMVC.Filters;
using MediConnectMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly MediConnectDbContext _context;

        public AccountController(MediConnectDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserName == model.UserName &&
                    u.Password == model.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");
            HttpContext.Session.SetInt32("UserID", user.UserID);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View(model);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists";
                return View(model);
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == model.RoleName);

            if (role == null)
            {
                ViewBag.Error = "Invalid role selected";
                return View(model);
            }

            var user = new MediConnectAPI.Models.User
            {
                UserName = model.UserName,
                Password = model.Password,
                RoleID = role.RoleID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patient = new Patient
            {
                Name = model.FullName,
                Email = model.Email,
                CPR = model.CPR,
                Phone = model.Phone,
                DOB = model.DOB,
                UserID = user.UserID
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(MediConnectAPI.Models.User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserName", user.UserName);

                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }
    }
}