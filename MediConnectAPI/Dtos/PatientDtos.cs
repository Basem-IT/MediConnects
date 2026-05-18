namespace MediConnectAPI.DTOs
{
    // Patient profile info
    public class PatientResponseDto
    {
        public int PatientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CPR { get; set; }
        public DateTime DOB { get; set; }
        public string Email { get; set; } = string.Empty;
        public int Phone { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
    }

    // Full history: patient and all their appointments as well as their medical records
    public class PatientHistoryDto
    {
        public PatientResponseDto Patient { get; set; } = null!;
        public List<AppointmentResponseDto> Appointments { get; set; } = new();
        public List<MedicalRecordResponseDto> MedicalRecords { get; set; } = new();
    }

    // Medical record (for summary) 
    public class MedicalRecordResponseDto
    {
        public int RecordID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string DoctorNotes { get; set; } = string.Empty;
        public string VisitSummary { get; set; } = string.Empty;
        public int AppointmentID { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public List<PrescriptionResponseDto> Prescriptions { get; set; } = new();
    }

    // Prescription summary 
    public class PrescriptionResponseDto
    {
        public int PrescriptionID { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime Duration { get; set; }
    }

    // Public lookup: what unauthenticated users see 
    // Intentionally limited to no emails and no full medical details
    public class PublicLookupRequestDto
    {
        public int CPR { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
    }

    public class PublicLookupResponseDto
    {
        // Only first name for privacy
        public string PatientFirstName { get; set; } = string.Empty;
        public List<PublicAppointmentDto> UpcomingAppointments { get; set; } = new();
        public List<PublicVisitSummaryDto> RecentVisits { get; set; } = new();
    }

    // Stripped-down appointment that is safe to show without logging in
    public class PublicAppointmentDto
    {
        public int AppointmentID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Stripped-down visit summary, diagnosis shown and detailed notes are not exposed
    public class PublicVisitSummaryDto
    {
        public DateTime VisitDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
    }
}