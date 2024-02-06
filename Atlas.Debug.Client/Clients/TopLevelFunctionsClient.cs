using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling debug functions hosted on the top-level `Atlas.Functions` app.
    /// </summary>
    public interface ITopLevelFunctionsClient
    {
        /// <summary>
        /// Peek messages from the `debug` subscription of the alerts service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<Alert>> PeekAlerts(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Peek messages from the `debug` subscription of the notifications service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<Notification>> PeekNotifications(PeekServiceBusMessagesRequest request);
    }

    /// <inheritdoc cref="ITopLevelFunctionsClient" />
    public class TopLevelFunctionsClient : HttpFunctionClient, ITopLevelFunctionsClient
    {
        /// <inheritdoc />
        public TopLevelFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<Alert>> PeekAlerts(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<Alert>>("debug/alerts", request);
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<Notification>> PeekNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<Notification>>("debug/notifications", request);
        }
    }
}