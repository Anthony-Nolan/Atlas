using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Clients
{
    public interface ISearchServiceBusClient
    {
        Task PublishToSearchQueue(IdentifiedSearchRequest searchRequest);
    }
    
    public class SearchServiceBusClient : ISearchServiceBusClient
    {
        private readonly string connectionString;
        private readonly string queueName;

        public SearchServiceBusClient(string connectionString, string queueName)
        {
            this.connectionString = connectionString;
            this.queueName = queueName;
        }

        public async Task PublishToSearchQueue(IdentifiedSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new Message(Encoding.UTF8.GetBytes(json));
            
            var client = new QueueClient(connectionString, queueName);
            await client.SendAsync(message);
        }
    }
}