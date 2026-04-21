namespace Atlas.MatchPrediction.Worker.Settings;

public class MatchPredictionWorkerSettings
{
    public required string RequestsSubscription { get; set; }

    public int BatchSize { get; set; } = 10;
}