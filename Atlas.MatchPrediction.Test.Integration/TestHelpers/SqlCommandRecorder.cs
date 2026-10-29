using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient.Diagnostics;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers;

/// <summary>
/// Records the SQL commands that SqlClient runs in this process, from its diagnostic events, until it is disposed.
/// </summary>
internal sealed class SqlCommandRecorder : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
{
    private const string SqlClientListenerName = "SqlClientDiagnosticListener";

    private readonly ConcurrentQueue<(string CommandText, string ConnectionString)> commands = new();
    private readonly ConcurrentQueue<IDisposable> listenerSubscriptions = new();
    private readonly IDisposable allListenersSubscription;

    public SqlCommandRecorder()
    {
        allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
    }

    /// <summary>The connection string of each command whose text contains <paramref name="text"/>, in the order they ran.</summary>
    public IReadOnlyList<string> ConnectionStringsOfCommandsThatContain(string text) =>
        [.. commands.Where(command => command.CommandText.Contains(text)).Select(command => command.ConnectionString)];

    public void Dispose()
    {
        allListenersSubscription.Dispose();
        while (listenerSubscriptions.TryDequeue(out var subscription))
        {
            subscription.Dispose();
        }
    }

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
    {
        if (listener.Name == SqlClientListenerName)
        {
            listenerSubscriptions.Enqueue(listener.Subscribe(this));
        }
    }

    void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> diagnosticEvent)
    {
        if (diagnosticEvent.Value is SqlClientCommandBefore commandBefore)
        {
            commands.Enqueue((commandBefore.Command.CommandText, commandBefore.Command.Connection.ConnectionString));
        }
    }

    void IObserver<DiagnosticListener>.OnCompleted()
    {
    }

    void IObserver<DiagnosticListener>.OnError(Exception error)
    {
    }

    void IObserver<KeyValuePair<string, object>>.OnCompleted()
    {
    }

    void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
    {
    }
}
