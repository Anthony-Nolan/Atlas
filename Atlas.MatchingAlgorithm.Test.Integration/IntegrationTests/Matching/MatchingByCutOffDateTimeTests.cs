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

        private AlleleLevelMatchCriteria matchCriteria;
        private PhenotypeInfo<string> matchingDonorHla;
        private PhenotypeInfo<string> nonMatchingDonorHla;
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

                SetupTestData();
            });
        }

        private void SetupTestData()
        {
            matchingDonorHla = new SampleTestHlas.HeterozygousSet1().ThreeLocus_SingleExpressingAlleles;
            nonMatchingDonorHla = new SampleTestHlas.HeterozygousSet2().ThreeLocus_SingleExpressingAlleles;

            var matchCriteriaBuilder = DependencyInjection.DependencyInjection.Provider.GetService<IMatchCriteriaBuilder>();
            var searchRequest = new SearchRequestFromHlasBuilder(matchingDonorHla).SixOutOfSix().Build();
            matchCriteria = Task.Run(() => matchCriteriaBuilder.BuildAlleleLevelMatchCriteria(searchRequest)).Result;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        [Test]
        public async Task GetMatches_MatchingDonorUpdatedOnMatchingCutOffDateTime_ReturnsDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(matchingDonorHla, CutOffDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
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
            var donorId = await CreateDonorInActiveDatabase(matchingDonorHla, donorModifiedDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
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
            var donorId = await CreateDonorInActiveDatabase(matchingDonorHla, donorModifiedDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NoCutOffDate_ReturnsMatchingDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(matchingDonorHla, CutOffDateTime);

            var matches = await matchingService.GetMatches(matchCriteria, null).ToListAsync();
            matches.ShouldContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NonMatchingDonorUpdatedOnMatchingCutOffDateTime_DoesNotReturnDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(nonMatchingDonorHla, CutOffDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
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
            var donorId = await CreateDonorInActiveDatabase(nonMatchingDonorHla, donorModifiedDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
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
            var donorId = await CreateDonorInActiveDatabase(nonMatchingDonorHla, donorModifiedDateTime);

            // TODO: ATLAS-843>ATLAS-917 change cut-off arg type to datetimeoffset
            var matches = await matchingService.GetMatches(matchCriteria, CutOffDateTime.DateTime).ToListAsync();
            matches.ShouldNotContainDonor(donorId);
        }

        [Test]
        public async Task GetMatches_NoCutOffDate_DoesNotReturnNonMatchingDonor()
        {
            var donorId = await CreateDonorInActiveDatabase(nonMatchingDonorHla, CutOffDateTime);

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

        private DonorAvailabilityUpdate BuildUpdate(int donorId, PhenotypeInfo<string> donorHla, DateTimeOffset updatedDateTime)
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
    }
}