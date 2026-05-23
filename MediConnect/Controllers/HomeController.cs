using System.Diagnostics;
using MediConnect.Models;
using MediConnectAPI.Data;
using MediConnectMVC.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MediConnectDbContext _context;

        public HomeController(ILogger<HomeController> logger, MediConnectDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.UnreadNotifications = _context.Notifications
                .Count(n => !n.IsRead);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}