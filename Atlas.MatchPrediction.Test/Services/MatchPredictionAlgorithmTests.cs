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
using Atlas.MatchPrediction.Services.Precompute;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

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
        private IDonorGenotypeSetSourceResolver genotypeSetSourceResolver;
        private IDonorGenotypeSetBatchCompleter genotypeSetBatchCompleter;
        private DonorGenotypeSetBatchContext batchContext;
        private IMatchPredictionAlgorithm matchPredictionAlgorithm;

        [SetUp]
        public void SetUp()
        {
            matchProbabilityService = Substitute.For<IMatchProbabilityService>();
            genotypeSetService = Substitute.For<IGenotypeSetService>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            resultUploader = Substitute.For<ISearchDonorResultUploader>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            genotypeSetSourceResolver = Substitute.For<IDonorGenotypeSetSourceResolver>();
            genotypeSetBatchCompleter = Substitute.For<IDonorGenotypeSetBatchCompleter>();
            matchPredictionAlgorithm = new MatchPredictionAlgorithm(
                matchProbabilityService,
                genotypeSetService,
                logger,
                haplotypeFrequencyService,
                resultUploader,
                genotypeSetSourceResolver,
                genotypeSetBatchCompleter);

            batchContext = DonorGenotypeSetBatchContext.Disabled(UsePrecomputedGenotypeSetsSource.FeatureFlag, null);
            genotypeSetSourceResolver.Resolve(default, default).ReturnsForAnyArgs(batchContext);

            haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(default)
                .ReturnsForAnyArgs(new HaplotypeFrequencySet());

            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            matchProbabilityService.CalculateMatchProbability(default, default).ReturnsForAnyArgs(
                new MatchProbabilityResult(new MatchProbabilityResponse(null, new HashSet<Locus>()), 0));
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
                Arg.Is<SubjectGenotypeSet>(x => ReferenceEquals(x, patientGenotypeSet)),
                Arg.Any<DonorGenotypeSetBatchContext>());
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
                Arg.Is<SubjectGenotypeSet>(x => ReferenceEquals(x, patientGenotypeSet)),
                Arg.Any<DonorGenotypeSetBatchContext>());
        }

        [Test]
        public async Task RunMatchPredictionAlgorithmBatch_ResolvesPrecomputeOncePassesItToEveryDonorAndCompletesAfterAllUploads()
        {
            var calls = new List<string>();
            resultUploader.WhenForAnyArgs(u => u.UploadSearchDonorResults(default, default, default)).Do(_ => calls.Add("upload"));
            genotypeSetBatchCompleter.WhenForAnyArgs(c => c.Complete(default, default, default)).Do(_ => calls.Add("complete"));
            matchProbabilityService.CalculateMatchProbability(default, default, default).ReturnsForAnyArgs(
                new MatchProbabilityResult(new MatchProbabilityResponse(null, new HashSet<Locus>()), 0, DonorGenotypeSetSource.NoRow));

            var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest { SearchRequestId = "search-request-id" })
            {
                Donors = new List<DonorInput>
                {
                    DonorInputBuilder.Default.WithDonorIds(1).Build(),
                    DonorInputBuilder.Default.WithDonorIds(2, 3).Build()
                }
            };

            await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(input);

            await genotypeSetSourceResolver.Received(1).Resolve(
                input,
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 1, 2, 3 })));
            await matchProbabilityService.Received(2).CalculateMatchProbability(
                Arg.Any<SingleDonorMatchProbabilityInput>(),
                Arg.Any<SubjectGenotypeSet>(),
                batchContext);
            await genotypeSetBatchCompleter.Received(1).Complete(
                input,
                batchContext,
                Arg.Is<IReadOnlyCollection<DonorGenotypeSetBatchOutcome>>(o =>
                    o.Count == 2 && o.All(x => x.Source == DonorGenotypeSetSource.NoRow) && o.Sum(x => x.DonorIdCount) == 3));
            calls.Should().Equal("upload", "upload", "complete");
        }

        [Test]
        public async Task RunMatchPredictionAlgorithm_ResolvesPrecomputeForTheDonorAndCompletes()
        {
            var input = SingleDonorMatchProbabilityInputBuilder.Valid.WithDonorId(5).Build();

            await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(input);

            await genotypeSetSourceResolver.Received(1).Resolve(input, Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 5 })));
            await genotypeSetBatchCompleter.Received(1).Complete(
                input,
                batchContext,
                Arg.Is<IReadOnlyCollection<DonorGenotypeSetBatchOutcome>>(o => o.Count == 1));
        }
    }
}
