using System.ComponentModel.DataAnnotations;

namespace Atlas.Common.Notifications;

public class NotificationsServiceBusSettings
{
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string AlertsTopic { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string NotificationsTopic { get; set; }

    [Range(0, int.MaxValue)]
    public int SendRetryCount { get; set; }

    [Range(0, int.MaxValue)]
    public int SendRetryCooldownSeconds { get; set; }
}