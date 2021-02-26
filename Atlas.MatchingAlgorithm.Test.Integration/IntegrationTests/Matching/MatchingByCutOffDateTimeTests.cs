using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingByCutOffDateTimeTests
    {
        private const DonorType DefaultDonorType = DonorType.Adult;
        private static readonly DateTimeOffset CutOffDateTime = new DateTimeOffset(2020, 1, 1, 1, 1, 1, TimeSpan.Zero);
        private static readonly PhenotypeInfo<string> MatchingDonorHla = new SampleTestHlas.HeterozygousSet1().ThreeLocus_SingleExpressingAlleles;
        private static readonly PhenotypeInfo<string> NonMatchingDonorHla = new SampleTestHlas.HeterozygousSet2().ThreeLocus_SingleExpressingAlleles;
        
        private TransientDatabase activeDb;
        private string hlaVersion;

        private IDonorManagementService donorManagementService;
        private IMatchingService matchingService;

        [SetUp]
        public void SetUp()
        {
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorManagementService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorManagementService>();

                var dbProvider = DependencyInjection.DependencyInjection.Provider.GetService<IActiveDatabaseProvider>();
                activeDb = dbProvider.GetActiveDatabase();

                var versionProvider = DependencyInjection.DependencyInjection.Provider.GetService<IActiveHlaNomenclatureVersionAccessor>();
                hlaVersion = versionProvider.GetActiveHlaNomenclatureVersion();
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        [Test]
        public async Task GetMatches_MatchingDonorUpdatedAtMatchingCutOffDateTime_ReturnsDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(MatchingDonorHla, CutOffDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldContainDonor(donorId);
        }

        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(1, 0, 0, 0)]
        public async Task GetMatches_MatchingDonorUpdatedAfterMatchingCutOffDateTime_ReturnsDonor(
            int daysIncrement,
            int hoursIncrement,
            int minutesIncrement,
            int secondsIncrement)
        {
            var donorModifiedDateTime = CutOffDateTime.Add(new TimeSpan(daysIncrement, hoursIncrement, minutesIncrement, secondsIncrement));
            var donorId = await CreateDonorInActiveDatabase(MatchingDonorHla, donorModifiedDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldContainDonor(donorId);
        }

        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(1, 0, 0, 0)]
        public async Task GetMatches_MatchingDonorUpdatedBeforeMatchingCutOffDateTime_DoesNotReturnDonor(
            int daysDecrement,
            int hoursDecrement,
            int minutesDecrement,
            int secondsDecrement)
        {
            var donorModifiedDateTime = CutOffDateTime.Subtract(new TimeSpan(daysDecrement, hoursDecrement, minutesDecrement, secondsDecrement));
            var donorId = await CreateDonorInActiveDatabase(MatchingDonorHla, donorModifiedDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NoCutOffDate_ReturnsMatchingDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(MatchingDonorHla, CutOffDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, null).ToListAsync();
            matches.ShouldContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NonMatchingDonorUpdatedAtMatchingCutOffDateTime_DoesNotReturnDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(NonMatchingDonorHla, CutOffDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(1, 0, 0, 0)]
        public async Task GetMatches_NonMatchingDonorUpdatedAfterMatchingCutOffDateTime_DoesNotReturnDonor(
            int daysIncrement,
            int hoursIncrement,
            int minutesIncrement,
            int secondsIncrement)
        {
            var donorModifiedDateTime = CutOffDateTime.Add(new TimeSpan(daysIncrement, hoursIncrement, minutesIncrement, secondsIncrement));
            var donorId = await CreateDonorInActiveDatabase(NonMatchingDonorHla, donorModifiedDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(1, 0, 0, 0)]
        public async Task GetMatches_NonMatchingDonorUpdatedBeforeMatchingCutOffDateTime_DoesNotReturnDonor(
            int daysDecrement,
            int hoursDecrement,
            int minutesDecrement,
            int secondsDecrement)
        {
            var donorModifiedDateTime = CutOffDateTime.Subtract(new TimeSpan(daysDecrement, hoursDecrement, minutesDecrement, secondsDecrement));
            var donorId = await CreateDonorInActiveDatabase(NonMatchingDonorHla, donorModifiedDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NoCutOffDate_DoesNotReturnNonMatchingDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(NonMatchingDonorHla, CutOffDateTime);
            var matchCriteria = await GetSixOutOfSixMatchCriteria();

            var matches = await matchingService.GetMatches(matchCriteria, null).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        private async Task<int> CreateDonorInActiveDatabase(PhenotypeInfo<string> donorHla, DateTimeOffset updateDateTime)
        {
            var donorId = DonorIdGenerator.NextId();
            var update = BuildUpdate(donorId, donorHla, updateDateTime);
            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { update }, activeDb, hlaVersion, false);

            return donorId;
        }

        private static DonorAvailabilityUpdate BuildUpdate(int donorId, PhenotypeInfo<string> donorHla, DateTimeOffset updatedDateTime)
        {
            var donorInfo = new DonorInfo
            {
                DonorId = donorId,
                DonorType = DefaultDonorType,
                HlaNames = donorHla
            };

            return new DonorAvailabilityUpdate
            {
                UpdateSequenceNumber = 12345,
                UpdateDateTime = updatedDateTime,
                DonorId = donorId,
                DonorInfo = donorInfo,
                IsAvailableForSearch = true
            };
        }

        private async Task<AlleleLevelMatchCriteria> GetSixOutOfSixMatchCriteria()
        {
            var matchCriteriaMapper = DependencyInjection.DependencyInjection.Provider.GetService<IMatchCriteriaMapper>();
            var searchRequest = new SearchRequestFromHlasBuilder(MatchingDonorHla).SixOutOfSix().Build();
            return await matchCriteriaMapper.MapRequestToAlleleLevelMatchCriteria(searchRequest);
        }
    }
}