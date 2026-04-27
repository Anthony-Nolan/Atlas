namespace Atlas.MatchPrediction.Worker.Settings;

public class MatchPredictionWorkerSettings
{
    public required string RequestsSubscription { get; set; }

    public required int BatchSize { get; set; }
}