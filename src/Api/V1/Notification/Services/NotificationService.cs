using BackBuddy.Api.Service.V1.Notification.Repositories; // Korrigierter Namespace 
using BackBuddy.Api.Service.V1.Notification.DTOs.Http;
using BackBuddy.Api.Service.V1.Notification.Exceptions;
using BackBuddy.Api.Service.V1.Notification.Entities;

namespace BackBuddy.Api.Service.V1.Notification.Services
{
    public interface INotificationService
    {
        Task SetFcmToken(string userId, string token, CancellationToken cancellationToken = default);
    }

    public partial class NotificationService(INotificationRepository repository) : INotificationService
    {

        private readonly INotificationRepository _repository = repository;
        public async Task SetFcmToken(string userId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new FcmTokenIsNullException();

            NotificationEntity entity = new()
            {
                UserId = userId,
                Token = token
            };

            await _repository.SetFcmToken(entity, cancellationToken);
        }
    }
}