using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.PublicApi.Functions;
using Atlas.Functions.PublicApi.Settings;
using Atlas.Functions.PublicApi.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.RepeatSearch.Services.Search;
using AwesomeAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
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
        private IAtlasLogger logger;
        private Dictionary<string, string> loggedProps;

        [SetUp]
        public void SetUp()
        {
            searchDispatcher = Substitute.For<ISearchDispatcher>();
            repeatSearchDispatcher = Substitute.For<IRepeatSearchDispatcher>();
            matchPredictionValidator = Substitute.For<IMatchPredictionValidator>();
            logger = Substitute.For<IAtlasLogger>();
            loggedProps = null;
            logger.When(l => l.SendEvent(SearchFunctions.PrecomputedGenotypeSetsChosenEventName, Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
                .Do(call => loggedProps = call.ArgAt<Dictionary<string, string>>(2));

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

            return new SearchFunctions(searchDispatcher, repeatSearchDispatcher, matchPredictionValidator, Options.Create(settings), logger);
        }

        private SearchFunctions BuildPrecomputeFunctions(bool defaultUsePrecomputedGenotypeSets, int requestPercentage, int? randomNumber = null)
        {
            var settings = new SearchFunctionSettings
            {
                DefaultUsePrecomputedGenotypeSets = defaultUsePrecomputedGenotypeSets,
                UsePrecomputedGenotypeSetsRequestPercentage = requestPercentage
            };

            return randomNumber == null
                ? new SearchFunctions(searchDispatcher, repeatSearchDispatcher, matchPredictionValidator, Options.Create(settings), logger)
                : new SearchFunctions(searchDispatcher, repeatSearchDispatcher, matchPredictionValidator, Options.Create(settings), logger, _ => randomNumber.Value);
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

        #region Precomputed genotype sets - chosen once, when the search is accepted

        [Test]
        public void Settings_ByDefault_AreOff()
        {
            var settings = new SearchFunctionSettings();

            settings.DefaultUsePrecomputedGenotypeSets.Should().BeFalse();
            settings.UsePrecomputedGenotypeSetsRequestPercentage.Should().Be(0);
        }

        [TestCase(true, false, 0)]
        [TestCase(false, true, 100)]
        public async Task Search_WhenUsePrecomputedGenotypeSetsIsSet_KeepsItAndIgnoresTheSettings(
            bool requestValue,
            bool defaultUsePrecomputedGenotypeSets,
            int requestPercentage)
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets, requestPercentage);

            await functions.Search(RequestBuilders.ValidSearchRequest(usePrecomputedGenotypeSets: requestValue).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.UsePrecomputedGenotypeSets == requestValue));
            loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("Request");
        }

        [TestCase(false, 100, false)]
        [TestCase(true, 0, false)]
        [TestCase(true, 100, true)]
        [TestCase(true, 150, true)]
        [TestCase(true, -5, false)]
        public async Task Search_WhenUsePrecomputedGenotypeSetsIsEmpty_ChoosesItFromTheSwitchAndTheShare(
            bool defaultUsePrecomputedGenotypeSets,
            int requestPercentage,
            bool expected)
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets, requestPercentage);

            await functions.Search(RequestBuilders.ValidSearchRequest(usePrecomputedGenotypeSets: null).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.UsePrecomputedGenotypeSets == expected));
            loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("FeatureFlag");
        }

        // The share is the percentage of searches with a random number (0-99) below it.
        [TestCase(1, 0, true)]
        [TestCase(1, 1, false)]
        [TestCase(1, 99, false)]
        [TestCase(50, 49, true)]
        [TestCase(50, 50, false)]
        [TestCase(99, 98, true)]
        [TestCase(99, 99, false)]
        public async Task Search_WhenUsePrecomputedGenotypeSetsIsEmpty_AndTheShareIsBetween1And99_UsesTheStoredSetsBelowTheShare(
            int requestPercentage,
            int randomNumber,
            bool expected)
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets: true, requestPercentage, randomNumber);

            await functions.Search(RequestBuilders.ValidSearchRequest(usePrecomputedGenotypeSets: null).ToHttpRequest());

            await searchDispatcher.Received(1).DispatchSearch(Arg.Is<SearchRequest>(r => r.UsePrecomputedGenotypeSets == expected));
        }

        [Test]
        public async Task Search_SendsOneEventWithTheChoiceAndTheSearchId()
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets: true, requestPercentage: 100);

            await functions.Search(RequestBuilders.ValidSearchRequest().ToHttpRequest());

            logger.Received(1).SendEvent(
                SearchFunctions.PrecomputedGenotypeSetsChosenEventName,
                Arg.Any<LogLevel>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>());
            loggedProps["SearchRequestId"].Should().Be("search-id");
            loggedProps["RepeatSearchRequestId"].Should().BeNull();
            loggedProps["UsePrecomputedGenotypeSets"].Should().Be("True");
        }

        [Test]
        public async Task Search_WhenTheEventFails_StillDispatchesAndReturnsTheSearchId()
        {
            logger.WhenForAnyArgs(l => l.SendEvent(default)).Do(_ => throw new Exception("app insights is down"));
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets: true, requestPercentage: 100);

            var result = await functions.Search(RequestBuilders.ValidSearchRequest().ToHttpRequest());

            result.Should().BeOfType<JsonResult>();
            await searchDispatcher.Received(1).DispatchSearchTrackingEvent(Arg.Any<SearchRequest>(), "search-id");
        }

        [Test]
        public async Task RepeatSearch_WhenUsePrecomputedGenotypeSetsIsEmpty_ChoosesItAgainFromTheSettings()
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets: true, requestPercentage: 100);
            var request = RequestBuilders.ValidRepeatSearchRequest(usePrecomputedGenotypeSets: null);

            await functions.RepeatSearch(request.ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.UsePrecomputedGenotypeSets == true));
            loggedProps["SearchRequestId"].Should().Be(request.OriginalSearchId);
            loggedProps["RepeatSearchRequestId"].Should().Be("repeat-search-id");
            loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("FeatureFlag");
        }

        [Test]
        public async Task RepeatSearch_WhenUsePrecomputedGenotypeSetsIsSet_KeepsIt()
        {
            var functions = BuildPrecomputeFunctions(defaultUsePrecomputedGenotypeSets: true, requestPercentage: 100);

            await functions.RepeatSearch(RequestBuilders.ValidRepeatSearchRequest(usePrecomputedGenotypeSets: false).ToHttpRequest());

            await repeatSearchDispatcher.Received(1)
                .DispatchSearch(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.UsePrecomputedGenotypeSets == false));
            loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("Request");
        }

        #endregion
    }
}
