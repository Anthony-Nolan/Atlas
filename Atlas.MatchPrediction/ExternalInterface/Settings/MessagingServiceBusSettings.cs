using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings;

public class MessagingServiceBusSettings
{
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; }

    [Range(0, int.MaxValue)]
    public int SendRetryCount { get; set; }

    [Range(0, int.MaxValue)]
    public int SendRetryCooldownSeconds { get; set; }
}