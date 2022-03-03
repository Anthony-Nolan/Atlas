using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    public class DonorFilteringTests
    {
        private ISearchService searchService;

        private DonorInfoWithExpandedHla donorAtRegistryA;
        private DonorInfoWithExpandedHla donorAtRegistryB;

        private const string RegistryCodeA = "registry-a";
        private const string RegistryCodeB = "registry-b";


        // A selection of valid hla data for the single donor to have
        private readonly PhenotypeInfo<string> donorHlas = new(
            valueA: new LocusInfo<string>("01:02", "01:02"),
            valueB: new LocusInfo<string>("14:53", "14:47"),
            valueDrb1: new LocusInfo<string>("13:03:01", "13:02:01:03"),
            valueC: new LocusInfo<string>("02:02", "02:02")
        );

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                    .BuildForActiveHlaNomenclatureVersion();
                var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo { HlaNames = donorHlas }).Result.MatchingHla;
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorRepository = repositoryFactory.GetDonorUpdateRepository();

                donorAtRegistryA = new DonorInfoWithExpandedHla
                {
                    DonorType = DonorType.Adult,
                    DonorId = DonorIdGenerator.NextId(),
                    HlaNames = donorHlas,
                    MatchingHla = matchingHlaPhenotype
                };
                donorAtRegistryB = new DonorInfoWithExpandedHla
                {
                    DonorType = DonorType.Adult,
                    DonorId = DonorIdGenerator.NextId(),
                    HlaNames = donorHlas,
                    MatchingHla = matchingHlaPhenotype
                };
                donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donorAtRegistryA, donorAtRegistryB }, false).Wait();

                ServiceConfiguration.MockDonorReader.GetDonors(Arg.Is<IEnumerable<int>>(ids =>
                        ids.ToHashSet().SetEquals(new HashSet<int> { donorAtRegistryA.DonorId, donorAtRegistryB.DonorId })
                    ))
                    .Returns(new Dictionary<int, Donor>
                    {
                        {
                            donorAtRegistryA.DonorId,
                            new Donor
                            {
                                AtlasDonorId = donorAtRegistryA.DonorId, ExternalDonorCode = donorAtRegistryA.DonorId.ToString(),
                                RegistryCode = RegistryCodeA
                            }
                        },
                        {
                            donorAtRegistryB.DonorId,
                            new Donor
                            {
                                AtlasDonorId = donorAtRegistryA.DonorId, ExternalDonorCode = donorAtRegistryA.DonorId.ToString(),
                                RegistryCode = RegistryCodeB
                            }
                        },
                    });
            });
        }

        [SetUp]
        public void SetUp()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        [Test]
        public async Task Search_WithIncludedRegistry_ReturnsOnlyDonorFromThatRegistry()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas).WithDonorRegistryCodes(new List<string> { RegistryCodeA }).Build();

            var results = (await searchService.Search(searchRequest)).ToList();

            results.Should().Contain(d => d.AtlasDonorId == donorAtRegistryA.DonorId);
            results.Should().NotContain(d => d.AtlasDonorId == donorAtRegistryB.DonorId);
        }

        [Test]
        public async Task Search_WithIncludedRegistry_AndDonorExistsWithMatchingRegistryInWrongCase_DonorIsNotReturned()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas).WithDonorRegistryCodes(new List<string> { RegistryCodeA.ToUpper() })
                .Build();

            var results = (await searchService.Search(searchRequest)).ToList();

            results.Should().NotContain(d => d.AtlasDonorId == donorAtRegistryA.DonorId);
        }

        [Test]
        public async Task Search_WithNoSpecifiedRegistries_DoesNotReturnAnyDonors()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas).WithDonorRegistryCodes(new List<string>()).Build();

            var results = (await searchService.Search(searchRequest)).ToList();

            results.Count.Should().Be(0);
        }

        [Test]
        public async Task Search_ForMatchingDonor_WithNullSpecifiedRegistries_ReturnsAllDonors()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas).WithDonorRegistryCodes(null).Build();

            var results = (await searchService.Search(searchRequest)).ToList();

            results.Should().Contain(d => d.AtlasDonorId == donorAtRegistryA.DonorId);
            results.Should().Contain(d => d.AtlasDonorId == donorAtRegistryB.DonorId);
        }
    }
}