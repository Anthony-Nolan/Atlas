using System.Threading;

namespace Atlas.Common.ApplicationInsights;

/// <summary>
/// Ambient holder for the current SearchRequestId, accessible across async boundaries.
/// Used by <see cref="SearchRequestTelemetryInitializer"/> to stamp correlation on all telemetry
/// without requiring access to scoped DI containers from singleton telemetry initializers.
/// </summary>
public static class SearchRequestContext
{
    private static readonly AsyncLocal<string> CurrentSearchRequestId = new();

    /// <summary>
    /// Gets or sets the SearchRequestId for the current async flow.
    /// Set this at the entry point of a request (e.g., Activity function, HTTP trigger).
    /// </summary>
    public static string SearchRequestId
    {
        get => CurrentSearchRequestId.Value;
        set => CurrentSearchRequestId.Value = value;
    }
}