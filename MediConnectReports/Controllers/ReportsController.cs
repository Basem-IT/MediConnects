using MediConnectReports.Models;
using MediConnectReports.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediConnectReports.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApiService _apiService;

        // service used to call backend api
        public ReportsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // main dashboard page for reports
        public async Task<IActionResult> Dashboard()
        {
            
            var token = HttpContext.Session.GetString("JwtToken");

            // go back to login page if not logged in it
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var dashboard = new DashboardViewModel();

            dashboard.Statistics =
                await _apiService.GetAsync<AppointmentStatisticsViewModel>(
                    "api/reports/appointment-statistics", token) ?? new();

            // get doctor 
            dashboard.DoctorUtilization =
                await _apiService.GetAsync<List<DoctorUtilizationViewModel>>(
                    "api/reports/doctor-utilization", token) ?? new();

            // get counts apptmnt
            dashboard.AppointmentsByStatus =
                await _apiService.GetAsync<List<StatusReportViewModel>>(
                    "api/reports/appointments-by-status", token) ?? new();

            // get latest 10 appointments for displayy
            dashboard.RecentAppointments =
                await _apiService.GetAsync<List<RecentAppointmentViewModel>>(
                    "api/reports/recent-appointments", token) ?? new();

            // missed appointment percent
            dashboard.MissedRate =
                await _apiService.GetAsync<MissedRateViewModel>(
                    "api/reports/missed-rate", token) ?? new();

            return View(dashboard);
        }
    }
}