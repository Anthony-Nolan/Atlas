using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Functions.PublicApi.Functions;
using Atlas.Functions.PublicApi.Settings;
using Atlas.Functions.PublicApi.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.RepeatSearch.Services.Search;
using AwesomeAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.PublicApi.Test.Functions
{
    [TestFixture]
    public class SearchFunctionsTests
    {
        private ISearchDispatcher searchDispatcher;
        private IRepeatSearchDispatcher repeatSearchDispatcher;
        private IMatchPredictionValidator matchPredictionValidator;

        [SetUp]
        public void SetUp()
        {
            searchDispatcher = Substitute.For<ISearchDispatcher>();
            repeatSearchDispatcher = Substitute.For<IRepeatSearchDispatcher>();
            matchPredictionValidator = Substitute.For<IMatchPredictionValidator>();

            searchDispatcher.DispatchSearch(Arg.Any<SearchRequest>()).Returns("search-id");
            repeatSearchDispatcher.DispatchSearch(Arg.Any<RepeatSearchRequest>()).Returns("repeat-search-id");

            // Non-donor input validation is out of scope for these tests; treat every request as valid.
            matchPredictionValidator.ValidateMatchProbabilityNonDonorInput(Arg.Any<SingleDonorMatchProbabilityInput>())
                .Returns(new ValidationResult());
        }

        private SearchFunctions BuildFunctions(bool defaultParallelMatchPrediction, int requestPercentage = 100)
        {
            var settings = new SearchFunctionSettings
            {
                DefaultParallelMatchPrediction = defaultParallelMatchPrediction,
                ParallelMatchPredictionRequestPercentage = requestPercentage
            };

            return new SearchFunctions(searchDispatcher, repeatSearchDispatcher, matchPredictionValidator, Options.Create(settings));
        }

        #region Search - explicit request value is honoured

        [Test]
        public async Task Search_WhenParallelMatchPredictionExplicitlyTrue_TakesParallelPath_EvenWhenServerSwitchOff()
        {
            // Server-side controls would otherwise resolve to false (master switch off, 0% canary).
            var functions = BuildFunctions(defaultParallelMatchPrediction: false, requestPercentage: 0);

            await functions.Search(RequestBuilders.ValidSearchRequest(parallelMatchPrediction: true).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == true));
        }

        [Test]
        public async Task Search_WhenParallelMatchPredictionExplicitlyFalse_TakesLegacyPath_EvenWhenServerSwitchOn()
        {
            // Server-side controls would otherwise resolve to true (master switch on, 100% canary).
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 100);

            await functions.Search(RequestBuilders.ValidSearchRequest(parallelMatchPrediction: false).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == false));
        }

        #endregion

        #region Search - null falls back to canary logic

        [Test]
        public async Task Search_WhenParallelMatchPredictionNull_AndServerSwitchOff_ResolvesToFalse()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: false, requestPercentage: 100);

            await functions.Search(RequestBuilders.ValidSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == false));
        }

        [Test]
        public async Task Search_WhenParallelMatchPredictionNull_AndServerSwitchOnWithFullPercentage_ResolvesToTrue()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 100);

            await functions.Search(RequestBuilders.ValidSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == true));
        }

        [Test]
        public async Task Search_WhenParallelMatchPredictionNull_AndServerSwitchOnWithZeroPercentage_ResolvesToFalse()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 0);

            await functions.Search(RequestBuilders.ValidSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == false));
        }

        #endregion

        #region RepeatSearch - explicit request value is honoured

        [Test]
        public async Task RepeatSearch_WhenParallelMatchPredictionExplicitlyTrue_TakesParallelPath_EvenWhenServerSwitchOff()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: false, requestPercentage: 0);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(parallelMatchPrediction: true).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == true));
        }

        [Test]
        public async Task RepeatSearch_WhenParallelMatchPredictionExplicitlyFalse_TakesLegacyPath_EvenWhenServerSwitchOn()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 100);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(parallelMatchPrediction: false).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == false));
        }

        #endregion

        #region RepeatSearch - null falls back to canary logic

        [Test]
        public async Task RepeatSearch_WhenParallelMatchPredictionNull_AndServerSwitchOff_ResolvesToFalse()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: false, requestPercentage: 100);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == false));
        }

        [Test]
        public async Task RepeatSearch_WhenParallelMatchPredictionNull_AndServerSwitchOnWithFullPercentage_ResolvesToTrue()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 100);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == true));
        }

        [Test]
        public async Task RepeatSearch_WhenParallelMatchPredictionNull_AndServerSwitchOnWithZeroPercentage_ResolvesToFalse()
        {
            var functions = BuildFunctions(defaultParallelMatchPrediction: true, requestPercentage: 0);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(parallelMatchPrediction: null).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == false));
        }

        #endregion
    }
}
