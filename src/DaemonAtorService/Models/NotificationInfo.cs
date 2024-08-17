using System;

namespace DaemonAtorService.Models;

public class NotificationData
{
    public NotificationInfo Notification { get; set; }
}

public class NotificationInfo
{
    public string Title { get; set; }
    public string Message { get; set; }
}
