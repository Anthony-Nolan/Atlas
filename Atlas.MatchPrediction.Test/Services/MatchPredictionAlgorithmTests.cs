using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Test.Services
{
    [TestFixture]
    internal class MatchPredictionAlgorithmTests
    {
        private IMatchProbabilityService matchProbabilityService;
        private IGenotypeSetService genotypeSetService;
        private IHaplotypeFrequencyService haplotypeFrequencyService;
        private ISearchDonorResultUploader resultUploader;
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private IServiceScopeFactory serviceScopeFactory;

        [SetUp]
        public void SetUp()
        {
            matchProbabilityService = Substitute.For<IMatchProbabilityService>();
            genotypeSetService = Substitute.For<IGenotypeSetService>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            resultUploader = Substitute.For<ISearchDonorResultUploader>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

            matchPredictionAlgorithm = new MatchPredictionAlgorithm(
                matchProbabilityService,
                genotypeSetService,
                logger,
                haplotypeFrequencyService,
                resultUploader, serviceScopeFactory);

            haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(default)
                .ReturnsForAnyArgs(new HaplotypeFrequencySet());

            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            matchProbabilityService.CalculateMatchProbability(default, default).ReturnsForAnyArgs(new MatchProbabilityResponse(null, new HashSet<Locus>()));
            resultUploader.UploadSearchDonorResults(default, default, default).ReturnsForAnyArgs(call =>
                ((IEnumerable<int>)call[1]).ToDictionary(id => id, id => $"{id}.json"));
        }

        [Test]
        public async Task RunMatchPredictionAlgorithmBatch_ExpandsPatientGenotypesOnceAndReusesThemForEachDonor()
        {
            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = "search-request-id",
                PatientHla = new PhenotypeInfo<string>("patient-hla").ToPhenotypeInfoTransfer()
            })
            {
                Donors = new List<DonorInput>
                {
                    DonorInputBuilder.Default.WithDonorIds(1).Build(),
                    DonorInputBuilder.Default.WithDonorIds(2).Build()
                }
            };

            await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(input);

            await genotypeSetService.Received(1).GetPatientGenotypeSet(
                Arg.Any<SingleDonorMatchProbabilityInput>());
            await matchProbabilityService.Received(2).CalculateMatchProbability(
                Arg.Any<SingleDonorMatchProbabilityInput>(),
                Arg.Is<SubjectGenotypeSet>(x => ReferenceEquals(x, patientGenotypeSet)));
        }

        [Test]
        public async Task RunMatchPredictionAlgorithm_GetsPatientGenotypeSetAndPassesToCalculateMatchProbability()
        {
            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(input);

            await genotypeSetService.Received(1).GetPatientGenotypeSet(
                Arg.Any<SingleDonorMatchProbabilityInput>());
            await matchProbabilityService.Received(1).CalculateMatchProbability(
                Arg.Any<SingleDonorMatchProbabilityInput>(),
                Arg.Is<SubjectGenotypeSet>(x => ReferenceEquals(x, patientGenotypeSet)));
        }
    }
}
