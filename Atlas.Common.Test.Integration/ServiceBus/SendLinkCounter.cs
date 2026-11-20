using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Azure.Core.Diagnostics;

namespace Atlas.Common.Test.Integration.ServiceBus;

/// <summary>
/// Counts the open send links of one topic, from the events of the Service Bus client library. The emulator does not show
/// links. The library reports each send link that it opens, and each send link that closes: when the sender or the
/// connection closes it, and when Service Bus detaches it.
/// </summary>
internal sealed class SendLinkCounter : IDisposable
{
    private const string ServiceBusEventSourceName = "Azure-Messaging-ServiceBus";

    // The ids of the events CreateSendLinkComplete and SendLinkClosed (Azure.Messaging.ServiceBus 7.20.1). The first item
    // of their payload is the identifier of the sender. If a new version of the library changes these ids, the counter
    // counts no links, and the test that expects open links fails.
    private const int SendLinkOpenedEventId = 81;
    private const int SendLinkClosedEventId = 100;

    private readonly string senderIdentifierPrefix;
    private readonly AzureEventSourceListener listener;
    private int opened;
    private int closed;

    public SendLinkCounter(string topicName)
    {
        // The default identifier of a sender is "<entity path>-<guid>".
        senderIdentifierPrefix = topicName + "-";
        listener = new AzureEventSourceListener(OnEvent, EventLevel.Informational);
    }

    /// <summary>The send links of the topic that opened and did not close since this counter started.</summary>
    public int Open => Volatile.Read(ref opened) - Volatile.Read(ref closed);

    public void Dispose() => listener.Dispose();

    private void OnEvent(EventWrittenEventArgs eventData, string message)
    {
        if (eventData.EventSource.Name != ServiceBusEventSourceName
            || eventData.Payload is not { Count: > 0 }
            || eventData.Payload[0] is not string senderIdentifier
            || !senderIdentifier.StartsWith(senderIdentifierPrefix, StringComparison.Ordinal))
        {
            return;
        }

        switch (eventData.EventId)
        {
            case SendLinkOpenedEventId:
                Interlocked.Increment(ref opened);
                break;
            case SendLinkClosedEventId:
                Interlocked.Increment(ref closed);
                break;
        }
    }
}
