using MediConnectReports.Models;
using MediConnectReports.Services;
using Microsoft.AspNetCore.Mvc;
namespace MediConnectReports.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApiService _apiService;
        public ReportsController(ApiService apiService)
        {
            _apiService = apiService;
        }
        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "ClinicManager")
                return RedirectToAction("Login", "Auth");
            var dashboard = new DashboardViewModel();
            dashboard.Statistics =
                await _apiService.GetAsync<AppointmentStatisticsViewModel>(
                    "api/reports/appointment-statistics", token) ?? new();
            dashboard.DoctorUtilization =
                await _apiService.GetAsync<List<DoctorUtilizationViewModel>>(
                    "api/reports/doctor-utilization", token) ?? new();
            dashboard.AppointmentsByStatus =
                await _apiService.GetAsync<List<StatusReportViewModel>>(
                    "api/reports/appointments-by-status", token) ?? new();
            dashboard.RecentAppointments =
                await _apiService.GetAsync<List<RecentAppointmentViewModel>>(
                    "api/reports/recent-appointments", token) ?? new();
            dashboard.MissedRate =
                await _apiService.GetAsync<MissedRateViewModel>(
                    "api/reports/missed-rate", token) ?? new();
            return View(dashboard);
        }
    }
}