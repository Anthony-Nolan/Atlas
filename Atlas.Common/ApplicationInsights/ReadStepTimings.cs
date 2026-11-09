using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Atlas.Common.ApplicationInsights;

/// <summary>
/// DEV timing only, do not merge (ATL-531): the time of each step of the read of a match prediction batch's stored
/// genotype sets. The resolver opens a scope around the read, and the reader and the repository add their steps to it.
/// With no scope, the methods do nothing, so the writers and the tests do not change.
/// </summary>
public static class ReadStepTimings
{
    private static readonly AsyncLocal<Dictionary<string, double>> Current = new();

    /// <summary>The physical connections that this process has seen.</summary>
    private static readonly ConcurrentDictionary<Guid, byte> SeenConnectionIds = new();

    public static bool IsActive => Current.Value != null;

    /// <summary>Starts a scope. Dispose the result to end it. The steps of the scope are in <paramref name="steps"/>.</summary>
    public static IDisposable Begin(out IReadOnlyDictionary<string, double> steps)
    {
        var scopeSteps = new Dictionary<string, double>();
        Current.Value = scopeSteps;
        steps = scopeSteps;
        return new Scope();
    }

    /// <summary>Adds the time of the stopwatch to the step, in milliseconds.</summary>
    public static void Add(string step, Stopwatch stopwatch) => Add(step, stopwatch.Elapsed.TotalMilliseconds);

    public static void Add(string step, double value)
    {
        var steps = Current.Value;
        if (steps != null)
        {
            steps[step] = steps.GetValueOrDefault(step) + value;
        }
    }

    /// <summary>
    /// <c>ReadNewConnection</c> is 1 when this process has not seen the connection before: the open made a new physical
    /// connection (TCP, TLS and login), not a pooled one.
    /// </summary>
    public static void AddConnection(Guid clientConnectionId)
    {
        if (IsActive)
        {
            Add("ReadNewConnection", SeenConnectionIds.TryAdd(clientConnectionId, 0) ? 1 : 0);
        }
    }

    /// <summary>Adds the SqlClient statistics of the read's connection (see <c>SqlConnection.RetrieveStatistics</c>).</summary>
    public static void AddSqlClientStatistics(IDictionary statistics)
    {
        if (!IsActive)
        {
            return;
        }

        Add("ReadServerRoundtrips", Statistic(statistics, "ServerRoundtrips"));
        Add("ReadNetworkServerMs", Statistic(statistics, "NetworkServerTime"));
        Add("ReadSqlClientExecutionMs", Statistic(statistics, "ExecutionTime"));
        Add("ReadBytesReceived", Statistic(statistics, "BytesReceived"));
    }

    private static double Statistic(IDictionary statistics, string name) =>
        statistics.Contains(name) ? Convert.ToDouble(statistics[name]) : -1;

    private sealed class Scope : IDisposable
    {
        public void Dispose() => Current.Value = null;
    }
}
