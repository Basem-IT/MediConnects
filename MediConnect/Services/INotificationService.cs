namespace MediConnectMVC.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int appointmentId, string message, string type);
    }
}