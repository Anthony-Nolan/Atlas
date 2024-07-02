using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.SearchTracking.Common.Clients;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    [TestFixture]
    public class MissingSearchLociTests
    {
        private ISearchDispatcher searchDispatcher;
        private PhenotypeInfo<string> searchHla;

        [SetUp]
        public void SetUp()
        {
            var searchServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchServiceBusClient>();
            var searchTrackingServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchTrackingServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchTrackingServiceBusClient);

            searchHla = new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles;
        }
        #region TenOutOfTen


        [Test]
        public void DispatchSearch_TenOutOfTen_PatientWithNullHlaAtLocusA_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .WithNullLocusSearchHlasAt(Locus.A)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_TenOutOfTen_PatientWithNullHlaAtLocusB_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .WithNullLocusSearchHlasAt(Locus.B)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_TenOutOfTen_PatientWithNullHlaAtLocusDrb1_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .WithNullLocusSearchHlasAt(Locus.Drb1)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_TenOutOfTen_PatientWithNullHlaAtLocusC_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .WithNullLocusSearchHlasAt(Locus.C)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_TenOutOfTen_PatientWithNullHlaAtLocusDqb1_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .WithNullLocusSearchHlasAt(Locus.Dqb1)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_TenOutOfTen_PatientTypedAtAllSearchLoci_DoesNotThrowValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .TenOutOfTen()
                .Build();

            Assert.DoesNotThrowAsync(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        #endregion

        #region SixOutOfSix

        [Test]
        public void DispatchSearch_SixOutOfSix_PatientWithNullHlaAtLocusA_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .SixOutOfSix()
                .WithNullLocusSearchHlasAt(Locus.A)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_SixOutOfSix_PatientWithNullHlaAtLocusB_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .SixOutOfSix()
                .WithNullLocusSearchHlasAt(Locus.B)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_SixOutOfSix_PatientWithNullHlaAtLocusDrb1_ThrowsValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .SixOutOfSix()
                .WithNullLocusSearchHlasAt(Locus.Drb1)
                .Build();

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_SixOutOfSix_PatientTypedAtAllSearchLoci_DoesNotThrowValidationError()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(searchHla)
                .SixOutOfSix()
                .Build();

            Assert.DoesNotThrowAsync(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        #endregion
    }
}
