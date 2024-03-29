﻿using Atlas.ManualTesting.Services.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.ManualTesting.Services
{
    public interface IMatchingRequestsPeeker
    {
        Task<IEnumerable<string>> GetIdsOfDeadLetteredMatchingRequests(PeekServiceBusMessagesRequest peekRequest);
    }

    internal class MatchingRequestsPeeker : IMatchingRequestsPeeker
    {
        private readonly IDeadLettersPeeker<IdentifiedSearchRequest> deadLetterReceiver;

        public MatchingRequestsPeeker(IDeadLettersPeeker<IdentifiedSearchRequest> deadLetterReceiver)
        {
            this.deadLetterReceiver = deadLetterReceiver;
        }

        public async Task<IEnumerable<string>> GetIdsOfDeadLetteredMatchingRequests(PeekServiceBusMessagesRequest peekRequest)
        {
            var deadLetteredRequests = await deadLetterReceiver.Peek(peekRequest);

            return deadLetteredRequests.PeekedMessages
                .Select(r => r.Id)
                .Distinct();
        }
    }
}