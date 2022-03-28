using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using AutoMapper;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IScoringRequestService
    {
        Task<ScoringResult> Score(DonorHlaScoringRequest scoringRequest);
        Task<List<DonorScoringResult>> ScoreBatch(BatchScoringRequest batchScoringRequest);
    }

    public class ScoringRequestService : IScoringRequestService
    {
        private readonly IDonorScoringService donorScoringService;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public ScoringRequestService(IDonorScoringService donorScoringService, IMapper mapper, ILogger logger)
        {
            this.donorScoringService = donorScoringService;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<ScoringResult> Score(DonorHlaScoringRequest scoringRequest)
        {
            var scoringResult = await donorScoringService.ScoreDonorHlaAgainstPatientHla(scoringRequest);

            return mapper.Map<ScoringResult>(scoringResult);
        }
        
        
        public async Task<List<DonorScoringResult>> ScoreBatch(BatchScoringRequest batchScoringRequest)
        {
            logger.SendTrace("Received ScoreBatch request");
            var patientPhenotypeInfo = batchScoringRequest.PatientHla.ToPhenotypeInfo();
            var donorPhenotypeMapping = batchScoringRequest.DonorsHla
                .Select(d => (DonorId: d.DonorId, DonorPhenotype: d.ToPhenotypeInfo()))
                .ToList();

            logger.SendTrace($"Starting to score a batch of {donorPhenotypeMapping.Count} donors", LogLevel.Info);

            var distinctDonorPhenotypes = donorPhenotypeMapping.Select(m => m.DonorPhenotype).Distinct().ToList();
            var scoringResults = await donorScoringService.ScoreDonorsHlaAgainstPatientHla(
                distinctDonorPhenotypes,
                patientPhenotypeInfo, 
                batchScoringRequest.ScoringCriteria);
            var donorScoringResults = donorPhenotypeMapping
                .Select(
                    donor =>
                    {
                        var scoringResult = scoringResults.Single(s => s.Key == donor.DonorPhenotype).Value;
                        return new DonorScoringResult()
                        {
                            DonorId = donor.DonorId,
                            ScoringResult = mapper.Map<ScoringResult>(scoringResult)
                        };
                    })
                .ToList();

            var donorIdsWithNullScoringResults = donorScoringResults.Where(s => s.ScoringResult == null).Select(s => s.DonorId).ToList();
            if (donorIdsWithNullScoringResults.Any())
            {
                logger.SendTrace($"Batch scoring has not returned results for several donors: {string.Join(", ", donorIdsWithNullScoringResults)}", LogLevel.Error);
            }

            logger.SendTrace($"Scoring a batch of {donorPhenotypeMapping.Count} donors has finished", LogLevel.Info);

            return donorScoringResults;
        }
    }
}