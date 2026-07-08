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
using Atlas.Common.Test.SharedTestHelpers.Builders;
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

            sut = new ParallelMatchPredictionAlgorithm(genotypeSetService, resultUploader, logger, serviceScopeFactory);

            var patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
            genotypeSetService.GetPatientGenotypeSet(default).ReturnsForAnyArgs(patientGenotypeSet);

            matchProbabilityService.CalculateMatchProbability(default, default).ReturnsForAnyArgs(new MatchProbabilityResponse(null, new HashSet<Locus>()));
            resultUploader.UploadBatchResult(default, default, default).ReturnsForAnyArgs("batch-result.json");

            serviceScopeFactory.CreateScope().Returns(_ => CreateMockScope());
        }

        private IServiceScope CreateMockScope()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IMatchProbabilityService)).Returns(matchProbabilityService);
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

            await sut.RunBatch(input, maxDegreeOfParallelism: 10, batchId: 1);

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

            await sut.RunBatch(input, maxDegreeOfParallelism: 1, batchId: 1);

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

            await sut.RunBatch(input, maxDegreeOfParallelism: 10, batchId: 1);

            serviceScopeFactory.Received(3).CreateScope();
        }

        [Test]
        public async Task RunBatch_UploadsAllDonorResultsInSingleFileAndReturnsItsLocation()
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

            var resultLocation = await sut.RunBatch(input, maxDegreeOfParallelism: 10, batchId: 42);

            Assert.That(resultLocation, Is.EqualTo("batch-result.json"));
            await resultUploader.Received(1).UploadBatchResult(
                "search-request-id",
                42,
                Arg.Is<IReadOnlyDictionary<int, MatchProbabilityResponse>>(d =>
                    d.Count == 2 && d.ContainsKey(1) && d.ContainsKey(2)));
        }

        [Test]
        public async Task RunBatch_WithNoDonors_ReturnsNullAndDoesNotUpload()
        {
            var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = "search-request-id",
                PatientHla = new PhenotypeInfo<string>("patient-hla").ToPhenotypeInfoTransfer()
            })
            {
                Donors = new List<DonorInput>()
            };

            var resultLocation = await sut.RunBatch(input, maxDegreeOfParallelism: 10, batchId: 42);

            Assert.That(resultLocation, Is.Null);
            await resultUploader.DidNotReceiveWithAnyArgs().UploadBatchResult(default, default, default);
        }
    }
}
