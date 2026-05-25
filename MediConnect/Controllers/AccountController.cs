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

        // database access setup
        public AccountController(MediConnectDbContext context)
        {
            _context = context;
        }

        // open login page
        public IActionResult Login()
        {
            return View();
        }

        // login check
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // find user by username and include role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                   u.UserName == model.UserName);

            // check if user exists and password matches
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            // (extra check but kinda repeated)
            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View(model);
            }

            // save user info in session
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");
            HttpContext.Session.SetInt32("UserID", user.UserID);

            return RedirectToAction("Index", "Home");
        }

        // logout and clear session
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // open register page
        public IActionResult Register()
        {
            return View();
        }

        // register new user + patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // check password confirmation
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View(model);
            }

            // check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists";
                return View(model);
            }

            // check if role is valid
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == model.RoleName);

            if (role == null)
            {
                ViewBag.Error = "Invalid role selected";
                return View(model);
            }

            // create new user account
            var user = new MediConnectAPI.Models.User
            {
                UserName = model.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleID = role.RoleID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // create patient profile linked to user
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

        // show user profile page
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            // if not logged in, go back to login
            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // update profile info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(MediConnectAPI.Models.User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // update session username
                HttpContext.Session.SetString("UserName", user.UserName);

                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }
    }
}