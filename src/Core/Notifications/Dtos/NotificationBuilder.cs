using FirebaseAdmin.Messaging;

namespace BackBuddy.Core.Library.Notifications.Dtos
{
    public class NotificationBuilder
    {
        private string? _title;
        private string? _body;
        private string? _imageUrl;

        public NotificationBuilder SetImageUrl(string imageUrl)
        {
            _imageUrl = imageUrl;
            return this;
        }

        public NotificationBuilder SetTitle(string title)
        {
            _title = title;
            return this;
        }

        public NotificationBuilder SetBody(string body)
        {
            _body = body;
            return this;
        }

        public Notification Build()
        {
            if (string.IsNullOrEmpty(_title))
            {
                throw new InvalidOperationException("Title must be set.");
            }
            if (string.IsNullOrEmpty(_body))
            {
                throw new InvalidOperationException("Body must be set.");
            }

            return new Notification()
            {
                Body = _body,
                Title = _title,
                ImageUrl = _imageUrl
            };
        }
    }
}
