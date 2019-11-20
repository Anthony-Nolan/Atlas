using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<IEnumerable<InputDonorWithExpandedHla>> ExpandDonorHlaBatchAsync(IEnumerable<InputDonor> inputDonors);
    }

    public class DonorHlaExpander : InputDonorBatchProcessor<InputDonorWithExpandedHla, MatchingDictionaryException>, IDonorHlaExpander
    {
        private const Priority LoggerPriority = Priority.Medium;
        private const string AlertSummary = "HLA Expansion Failure(s) in Search Algorithm";

        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;

        public DonorHlaExpander(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            ILogger logger,
            INotificationsClient notificationsClient)
            : base(logger, notificationsClient, LoggerPriority, AlertSummary)
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
        }

        public async Task<IEnumerable<InputDonorWithExpandedHla>> ExpandDonorHlaBatchAsync(IEnumerable<InputDonor> inputDonors)
        {
            return await ProcessBatchAsync(
                inputDonors,
                async d => await CombineDonorAndExpandedHla(d),
                (exception, donor) => new MatchingDictionaryLookupFailureEventModel(exception, $"{donor.DonorId}"));
        }

        private async Task<InputDonorWithExpandedHla> CombineDonorAndExpandedHla(InputDonor inputDonor)
        {
            var hla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(
                new PhenotypeInfo<string>(inputDonor.HlaNames));

            return new InputDonorWithExpandedHla
            {
                DonorId = inputDonor.DonorId,
                DonorType = inputDonor.DonorType,
                RegistryCode = inputDonor.RegistryCode,
                MatchingHla = hla,
            };
        }
    }
}
