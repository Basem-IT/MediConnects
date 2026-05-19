namespace MediConnectReports.Models
{
    public class AppointmentStatisticsViewModel
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int MissedAppointments { get; set; }
    }

    public class DoctorUtilizationViewModel
    {
        public string DoctorName { get; set; } = "";
        public int AppointmentCount { get; set; }
    }

    public class StatusReportViewModel
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }

    public class RecentAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string PatientName { get; set; } = "";
    }

    public class MissedRateViewModel
    {
        public int MissedAppointments { get; set; }
        public double MissedRate { get; set; }
    }

    public class DashboardViewModel
    {
        public AppointmentStatisticsViewModel Statistics { get; set; } = new();
        public List<DoctorUtilizationViewModel> DoctorUtilization { get; set; } = new();
        public List<StatusReportViewModel> AppointmentsByStatus { get; set; } = new();
        public List<RecentAppointmentViewModel> RecentAppointments { get; set; } = new();
        public MissedRateViewModel MissedRate { get; set; } = new();
    }
}