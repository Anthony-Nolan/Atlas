 using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration
{
    internal interface IMaskedSimulantsGenerator
    {
        /// <summary>
        /// Generates (and stores) phenotypes by masking simulated genotypes.
        /// </summary>
        Task GenerateSimulants(GenerateSimulantsRequest request, MaskingRequests maskingRequests, string hlaNomenclatureVersion);
    }

    internal class MaskedSimulantsGenerator : SimulantsGeneratorBase, IMaskedSimulantsGenerator
    {
        private readonly ILocusHlaMasker locusHlaMasker;
        private readonly ITestHarnessRepository testHarnessRepository;

        public MaskedSimulantsGenerator(
            ILocusHlaMasker locusHlaMasker,
            ITestHarnessRepository testHarnessRepository,
            ISimulantsRepository simulantsRepository)
        : base(simulantsRepository)
        {
            this.locusHlaMasker = locusHlaMasker;
            this.testHarnessRepository = testHarnessRepository;
        }

        public async Task GenerateSimulants(
            GenerateSimulantsRequest request,
            MaskingRequests maskingRequests,
            string hlaNomenclatureVersion)
        {
            Debug.WriteLine($"Masking {request.TestIndividualCategory} genotypes.");

            var maskedLoci = await MaskGenotypesByLocus(request, maskingRequests, hlaNomenclatureVersion);
            var maskedSimulants = BuildSimulantsFromMaskedLoci(maskedLoci, request);

            await StoreSimulants(maskedSimulants);
            await WriteMaskingRecords(request, maskingRequests);
        }

        private async Task<IReadOnlyCollection<SimulantLocusHla>> MaskGenotypesByLocus(
            GenerateSimulantsRequest request,
            MaskingRequests maskingRequests,
            string hlaNomenclatureVersion)
        {
            var genotypeSimulants = (await ReadGenotypeSimulants(request.TestHarnessId, request.TestIndividualCategory))
                .Select(s => new { GenotypeId = s.Id, HlaInfo = s.ToPhenotypeInfo() })
                .ToList();

            if (genotypeSimulants.Count != request.SimulantCount)
            {
                throw new Exception($"Problem when reading stored genotypes for test harness, {request.TestHarnessId}. " +
                                    $"Expected {request.SimulantCount}, but retrieved {genotypeSimulants.Count}.");
            }

            var results = await maskingRequests.MapAsync(async (locus, requests) =>
            {
                if (!MatchPredictionStaticData.MatchPredictionLoci.Contains(locus))
                {
                    return new List<SimulantLocusHla>();
                }

                var locusRequest = new LocusMaskingRequests
                {
                    MaskingRequests = requests,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    TotalSimulantCount = request.SimulantCount
                };

                var typings = genotypeSimulants.Select(s => new SimulantLocusHla
                {
                    GenotypeSimulantId = s.GenotypeId,
                    Locus = locus,
                    HlaTyping = s.HlaInfo.GetLocus(locus)
                }).ToList();

                return await locusHlaMasker.MaskHlaForSingleLocus(locusRequest, typings);
            });
            
            return MatchPredictionStaticData.MatchPredictionLoci.SelectMany(locus => results.GetLocus(locus)).ToList();
        }

        private static IReadOnlyCollection<Simulant> BuildSimulantsFromMaskedLoci(
            IEnumerable<SimulantLocusHla> maskedLoci,
            GenerateSimulantsRequest request)
        {
            return maskedLoci
                .GroupBy(ml => ml.GenotypeSimulantId)
                .Select(simulantLociHla => MapToSimulantDatabaseModel(
                    request,
                    SimulatedHlaTypingCategory.Masked,
                    simulantLociHla.ToSimulatedHlaTyping(),
                    simulantLociHla.Key))
                .ToList();
        }

        private async Task WriteMaskingRecords(GenerateSimulantsRequest request, MaskingRequests maskingRequests)
        {
            var records = MatchPredictionStaticData.MatchPredictionLoci.Select(l =>
            {
                var locusRequests = maskingRequests
                    .GetLocus(l)
                    ?.Where(r => r.ProportionToMask > 0);

                return new MaskingRecord
                {
                    TestHarness_Id = request.TestHarnessId,
                    TestIndividualCategory = request.TestIndividualCategory,
                    Locus = l,
                    MaskingRequests = locusRequests.IsNullOrEmpty() ? "No Requests" : JsonConvert.SerializeObject(locusRequests)
                };
            });

            await testHarnessRepository.AddMaskingRecords(records);
        }
    }
}
