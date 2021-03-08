using FileExplorer.Core;

namespace FileExplorer.Messages
{
    public class NotificationMessage
    {
        public string Path { get; set; }

        public string NewPath { get; set; }

        public NotificationType NotificationType { get; set; }
    }
}
