﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class RepeatSearchFunctions
    {
        private readonly IRepeatSearchDispatcher repeatSearchDispatcher;
        private readonly IRepeatSearchRunner repeatSearchRunner;

        public RepeatSearchFunctions(IRepeatSearchDispatcher repeatSearchDispatcher, IRepeatSearchRunner repeatSearchRunner)
        {
            this.repeatSearchDispatcher = repeatSearchDispatcher;
            this.repeatSearchRunner = repeatSearchRunner;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(InitiateRepeatSearch))]
        public async Task<IActionResult> InitiateRepeatSearch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var repeatSearchRequest = JsonConvert.DeserializeObject<RepeatSearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            try
            {
                var id = await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);
                return new JsonResult(new SearchInitiationResponse { SearchIdentifier = id });
            }
            catch (ValidationException e)
            {
                return new BadRequestObjectResult(e.ToValidationErrorsModel());
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunRepeatSearch))]
        public async Task RunRepeatSearch(
            [ServiceBusTrigger(
                "%MessagingServiceBus:RepeatSearchRequestsTopic%",
                "%MessagingServiceBus:RepeatSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            Message message)
        {
            var serialisedData = Encoding.UTF8.GetString(message.Body);
            var request = JsonConvert.DeserializeObject<IdentifiedRepeatSearchRequest>(serialisedData);

            await repeatSearchRunner.RunSearch(request);
        }
    }
}