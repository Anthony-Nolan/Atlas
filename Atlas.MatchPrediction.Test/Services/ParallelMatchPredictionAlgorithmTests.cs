using System;
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
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Test.Services
{
    [TestFixture]
    internal class ParallelMatchPredictionAlgorithmTests
    {
        private IMatchProbabilityService matchProbabilityService;
        private IGenotypeSetService genotypeSetService;
        private ISearchDonorResultUploader resultUploader;
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private IServiceScopeFactory serviceScopeFactory;
        private IParallelMatchPredictionAlgorithm sut;

        [SetUp]
        public void SetUp()
        {
            matchProbabilityService = Substitute.For<IMatchProbabilityService>();
            genotypeSetService = Substitute.For<IGenotypeSetService>();
            resultUploader = Substitute.For<ISearchDonorResultUploader>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

            sut = new ParallelMatchPredictionAlgorithm(genotypeSetService, logger, serviceScopeFactory);

            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            matchProbabilityService.CalculateMatchProbability(default, default).ReturnsForAnyArgs(new MatchProbabilityResponse(null, new HashSet<Locus>()));
            resultUploader.UploadSearchDonorResults(default, default, default).ReturnsForAnyArgs(call =>
                ((IEnumerable<int>)call[1]).ToDictionary(id => id, id => $"{id}.json"));

            serviceScopeFactory.CreateScope().Returns(_ => CreateMockScope());
        }

        private IServiceScope CreateMockScope()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IMatchProbabilityService)).Returns(matchProbabilityService);
            serviceProvider.GetService(typeof(ISearchDonorResultUploader)).Returns(resultUploader);
            serviceProvider.GetService(typeof(IMatchPredictionLogger<MatchProbabilityLoggingContext>)).Returns(logger);
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            return scope;
        }

        [Test]
        public async Task RunBatch_ExpandsPatientGenotypesOnceAndReusesThemForEachDonor()
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

            await sut.RunBatch(input, maxDegreeOfParallelism: 10);

            await genotypeSetService.Received(1).GetPatientGenotypeSet(Arg.Any<SingleDonorMatchProbabilityInput>());
            await matchProbabilityService.Received(2).CalculateMatchProbability(
                Arg.Any<SingleDonorMatchProbabilityInput>(),
                Arg.Is<SubjectGenotypeSet>(x => ReferenceEquals(x, patientGenotypeSet)));
        }

        [Test]
        public async Task RunBatch_ProcessesAllDonorsWithConstrainedParallelism()
        {
            var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = "search-request-id",
                PatientHla = new PhenotypeInfo<string>("patient-hla").ToPhenotypeInfoTransfer()
            })
            {
                Donors = new List<DonorInput>
                {
                    DonorInputBuilder.Default.WithDonorIds(1).Build(),
                    DonorInputBuilder.Default.WithDonorIds(2).Build(),
                    DonorInputBuilder.Default.WithDonorIds(3).Build()
                }
            };

            await sut.RunBatch(input, maxDegreeOfParallelism: 1);

            await matchProbabilityService.Received(3).CalculateMatchProbability(
                Arg.Any<SingleDonorMatchProbabilityInput>(),
                Arg.Any<SubjectGenotypeSet>());
        }

        [Test]
        public async Task RunBatch_CreatesNewScopePerDonor()
        {
            var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = "search-request-id",
                PatientHla = new PhenotypeInfo<string>("patient-hla").ToPhenotypeInfoTransfer()
            })
            {
                Donors = new List<DonorInput>
                {
                    DonorInputBuilder.Default.WithDonorIds(1).Build(),
                    DonorInputBuilder.Default.WithDonorIds(2).Build(),
                    DonorInputBuilder.Default.WithDonorIds(3).Build()
                }
            };

            await sut.RunBatch(input, maxDegreeOfParallelism: 10);

            serviceScopeFactory.Received(3).CreateScope();
        }

        [Test]
        public async Task RunBatch_AggregatesAllDonorResults()
        {
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

            var results = await sut.RunBatch(input, maxDegreeOfParallelism: 10);

            Assert.That(results.Keys, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(results[1], Is.EqualTo("1.json"));
            Assert.That(results[2], Is.EqualTo("2.json"));
        }
    }
}
