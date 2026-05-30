namespace MediConnectReports.Models
{
    public class LoginViewModel
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string? ErrorMessage { get; set; }
    }

    public class LoginResultModel
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}