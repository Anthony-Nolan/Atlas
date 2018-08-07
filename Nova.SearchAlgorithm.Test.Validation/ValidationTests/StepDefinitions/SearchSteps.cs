using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.ValidationTests;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [Binding]
    public class SearchSteps
    {
        [Given(@"I search for exact donor hla")]
        public void GivenISearchForRecognisedHla()
        {
            var patientGenotypeHla = GenotypeRepository.Genotypes.First().Hla;
            
            var searchRequestBuilder = new SearchRequestBuilder()
                .WithLocusMatchHla(Locus.A, TypePositions.One, patientGenotypeHla.A_1.TgsTypedAllele)
                .WithLocusMatchHla(Locus.A, TypePositions.Two, patientGenotypeHla.A_2.TgsTypedAllele)
                .WithLocusMatchHla(Locus.B, TypePositions.One, patientGenotypeHla.B_1.TgsTypedAllele)
                .WithLocusMatchHla(Locus.B, TypePositions.Two, patientGenotypeHla.B_2.TgsTypedAllele)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, patientGenotypeHla.DRB1_1.TgsTypedAllele)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, patientGenotypeHla.DRB1_2.TgsTypedAllele)
                .WithLocusMatchHla(Locus.C, TypePositions.One, patientGenotypeHla.C_1.TgsTypedAllele)
                .WithLocusMatchHla(Locus.C, TypePositions.Two, patientGenotypeHla.C_2.TgsTypedAllele)
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.One, patientGenotypeHla.DQB1_1.TgsTypedAllele)
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.Two, patientGenotypeHla.DQB1_2.TgsTypedAllele);
            
            ScenarioContext.Current.Set(searchRequestBuilder);
        }

        [Given(@"The search type is (.*)")]
        public void GivenTheSearchTypeIs(string searchType)
        {
            var donorType = (DonorType) Enum.Parse(typeof(DonorType), searchType, true);
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.WithSearchType(donorType));
        }

        [Given(@"The search is run for Anthony Nolan's registry only")]
        public void GivenTheSearchIsRunForAnthonyNolansRegistryOnly()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.ForRegistries(new []{ RegistryCode.AN }));
        }

        [Given(@"The search is run for aligned registries")]
        public void GivenTheSearchIsRunForAlignedRegistries()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.ForRegistries(new []
            {
                RegistryCode.AN,
                RegistryCode.NHSBT,
                RegistryCode.WBS,
                RegistryCode.DKMS
            }));
        }

        [Given(@"The search is run for the registry: (.*)")]
        public void GivenTheSearchIsRunForRegistry(string registryString)
        {
            // If the search team prefer to write the tests with expanded registry names, we will need to manually map to the enum
            var registry = (RegistryCode) Enum.Parse(typeof(RegistryCode), registryString, true);
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.ForAdditionalRegistry(registry));
        }

        [When(@"I run a 6/6 search")]
        public async Task WhenIRunASixOutOfSixSearch()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }
        
        [When(@"I run an 8/8 search")]
        public async Task WhenIRunAnEightOutOfEightSearch()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }
               
        [When(@"I run a 4/8 search")]
        public async Task WhenIRunAFourOutOfEightSearch()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(4)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 2)
                .WithLocusMismatchCount(Locus.Drb1, 2)
                .WithLocusMismatchCount(Locus.C, 2)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }
        
        [When(@"I run a 10/10 search")]
        public async Task WhenIRunATenOutOfTenSearch()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .WithLocusMismatchCount(Locus.Dqb1, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }   
        
        [When(@"I run a 9/10 search at locus (.*)")]
        public async Task WhenIRunANineOutOfTenSearchAtLocus(string locusString)
        {
            var locus = (Locus) Enum.Parse(typeof(Locus), locusString, true);
            var exactMatchLoci = LocusHelpers.AllLoci().Except(new[] {Locus.Dpb1, locus});
            
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(1)
                .WithLocusMismatchCount(locus, 1)
                .WithMismatchCountAtLoci(exactMatchLoci, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }
        
        [When(@"I run an 8/10 search at locus (.*)")]
        public async Task WhenIRunAnEightOutOfTenSearchAtLocus(string locusString)
        {
            var locus = (Locus) Enum.Parse(typeof(Locus), locusString, true);
            var exactMatchLoci = LocusHelpers.AllLoci().Except(new[] {Locus.Dpb1, locus});
            
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(2)
                .WithLocusMismatchCount(locus, 2)
                .WithMismatchCountAtLoci(exactMatchLoci, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [Then(@"The result should contain at least one donor")]
        public void ThenTheResultShouldContainAtLeastOneDonor()
        {
            var results = ScenarioContext.Current.Get<SearchResultSet>();
            results.SearchResults.Count().Should().BeGreaterThan(0);
        }
        
        [Then(@"The result should contain no donors")]
        public void ThenTheResultShouldContainNoDonors()
        {
            var results = ScenarioContext.Current.Get<SearchResultSet>();
            results.SearchResults.Count().Should().Be(0);
        }
    }
}