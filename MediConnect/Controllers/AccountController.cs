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

        // inject db context
        public AccountController(MediConnectDbContext context)
        {
            _context = context;
        }

        // show login page
        public IActionResult Login()
        {
            return View();
        }

        // handle login form submission
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // find user by username and load their role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            // check password using bcrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            // save to session so we know who is logged in
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");
            HttpContext.Session.SetInt32("UserID", user.UserID);

            return RedirectToAction("Index", "Home");
        }

        // clear session and go back to login
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // show register page
        public IActionResult Register()
        {
            return View();
        }

        // handle register form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // make sure passwords match
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View(model);
            }

            // check if username is taken
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists";
                return View(model);
            }

            // make sure role exists
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == model.RoleName);

            if (role == null)
            {
                ViewBag.Error = "Invalid role selected";
                return View(model);
            }

            // create user with hashed password
            var user = new MediConnectAPI.Models.User
            {
                UserName = model.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleID = role.RoleID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // create patient profile linked to this user
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

        // show profile page for logged in user
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            // redirect if not logged in
            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // save profile changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(MediConnectAPI.Models.User user)
        {
            ModelState.Remove("Password");
            ModelState.Remove("Role");
            ModelState.Remove("Staffs");

            if (ModelState.IsValid)
            {
                // update only the username field
                var existing = await _context.Users.FindAsync(user.UserID);
                if (existing != null)
                {
                    existing.UserName = user.UserName;
                    await _context.SaveChangesAsync();

                    // update session so navbar shows new name
                    HttpContext.Session.SetString("UserName", user.UserName);
                }

                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }
    }
}