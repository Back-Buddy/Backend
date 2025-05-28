using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackBuddy.Api.Service.V1.Notification.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Notification.Repositories
{
    public interface INotificationRepository
    {
        Task SetFcmToken(NotificationEntity entity, CancellationToken cancellationToken = default);
    }

    public class NotificationRepository(IMongoCollection<NotificationEntity> collection) : INotificationRepository
    {
        private readonly IMongoCollection<NotificationEntity> _collection = collection;
        public async Task SetFcmToken(NotificationEntity entity, CancellationToken cancellationToken = default)
        {
            FilterDefinition<NotificationEntity> filter = Builders<NotificationEntity>.Filter.Eq(x => x.UserId, entity.UserId);
            UpdateDefinition<NotificationEntity> update = Builders<NotificationEntity>.Update.Set(x => x.Token, entity.Token);
            var options = new UpdateOptions { IsUpsert = true };
            await _collection.UpdateOneAsync(filter, update, options, cancellationToken: cancellationToken);
        }    
    }
}