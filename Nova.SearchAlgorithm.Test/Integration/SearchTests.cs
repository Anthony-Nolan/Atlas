using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.TestUtils.Assertions;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [TestFixture]
    public class SearchTests : IntegrationTestBase
    {
        private IDonorImportService donorImportService;
        private ISearchService searchService;

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            // TODO: We probably want use the DonorRepo directly to import
            // bespoke test donors relevant to each test classes' test suite.
            // Using ImportSingleTestDonor here is a shortcut to prove we can insert data.
            donorImportService = container.Resolve<IDonorImportService>();
            donorImportService.ImportSingleTestDonor();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = container.Resolve<ISearchService>();
        }

        [Test]
        public void TestSomething()
        {
            IEnumerable<DonorMatch> results = searchService.Search(new SearchRequest
            {
            });

            results.Should().Contain(d => d.DonorId == 1);
        }
    }
}
