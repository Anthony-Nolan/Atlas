using System;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Services.Search;
using AutoFixture;
using AwesomeAssertions;
using FluentValidation;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.Search
{
    [TestFixture]
    public class RepeatSearchValidatorTests
    {
        private ICanonicalResultSetRepository canonicalResultSetRepository;

        private IRepeatSearchValidator repeatSearchValidator;
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();

            canonicalResultSetRepository.GetCanonicalResultSetSummary(default).ReturnsForAnyArgs(new CanonicalResultSet());
            
            repeatSearchValidator = new RepeatSearchValidator(canonicalResultSetRepository);
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_WhenRequiredDataMissing_ThrowsException()
        {
            var repeatSearch = new RepeatSearchRequest();

            await repeatSearchValidator
                .Invoking(v => v.ValidateRepeatSearchAndThrow(repeatSearch))
                .Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_ForValidRequest_DoesNotThrowException()
        {
            var repeatSearch = new RepeatSearchRequest
            {
                SearchRequest = new SearchRequestBuilder().Build(),
                OriginalSearchId = "123",
                SearchCutoffDate = DateTimeOffset.Now
            };

            await repeatSearchValidator
                .Invoking(async v => await v.ValidateRepeatSearchAndThrow(repeatSearch))
                .Should().NotThrowAsync();
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_WithUnrecognisedSearchId_ThrowsException()
        {
            const string searchId = "invalid-search-request";
            
            var repeatSearch = new RepeatSearchRequest
            {
                SearchRequest = new SearchRequestBuilder().Build(),
                OriginalSearchId = searchId,
                SearchCutoffDate = DateTimeOffset.Now
            };

            canonicalResultSetRepository.GetCanonicalResultSetSummary(searchId).Returns(null as CanonicalResultSet);

            await repeatSearchValidator
                .Invoking(v => v.ValidateRepeatSearchAndThrow(repeatSearch))
                .Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task IsOriginalSearchResultSetStored_WhenResultSetIsStored_ReturnsTrue()
        {
            var isStored = await repeatSearchValidator.IsOriginalSearchResultSetStored(fixture.Create<string>());

            isStored.Should().BeTrue();
        }

        [Test]
        public async Task IsOriginalSearchResultSetStored_WhenResultSetIsNotStored_ReturnsFalse()
        {
            var searchId = fixture.Create<string>();
            canonicalResultSetRepository.GetCanonicalResultSetSummary(searchId).Returns(null as CanonicalResultSet);

            var isStored = await repeatSearchValidator.IsOriginalSearchResultSetStored(searchId);

            isStored.Should().BeFalse();
        }
    }
}