namespace MediConnectMVC.Services
{
    public class NotificationService : INotificationService
    {
        public Task CreateNotificationAsync(int appointmentId, string message, string type)
        {
            return Task.CompletedTask;
        }
    }
}