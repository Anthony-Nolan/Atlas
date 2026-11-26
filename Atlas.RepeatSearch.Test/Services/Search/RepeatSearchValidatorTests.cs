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

            canonicalResultSetRepository.GetCanonicalResultSetSummary(default)
                .ReturnsForAnyArgs(new CanonicalResultSet { Status = CanonicalResultSetStatus.Complete });
            canonicalResultSetRepository.MarkResultSetUsed(default).ReturnsForAnyArgs(true);

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

            // The repository cannot mark a set that is not stored: there is no complete set with this search id.
            canonicalResultSetRepository.MarkResultSetUsed(searchId).Returns(false);

            await repeatSearchValidator
                .Invoking(v => v.ValidateRepeatSearchAndThrow(repeatSearch))
                .Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_ForValidRequest_MarksOriginalResultSetUsed()
        {
            var repeatSearch = new RepeatSearchRequest
            {
                SearchRequest = new SearchRequestBuilder().Build(),
                OriginalSearchId = fixture.Create<string>(),
                SearchCutoffDate = fixture.Create<DateTimeOffset>()
            };

            await repeatSearchValidator.ValidateRepeatSearchAndThrow(repeatSearch);

            await canonicalResultSetRepository.Received(1).MarkResultSetUsed(repeatSearch.OriginalSearchId);
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_WhenRequiredDataMissing_DoesNotMarkResultSetUsed()
        {
            var act = () => repeatSearchValidator.ValidateRepeatSearchAndThrow(new RepeatSearchRequest());

            await act.Should().ThrowAsync<ValidationException>();
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().MarkResultSetUsed(default);
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

        [TestCase(CanonicalResultSetStatus.InProgress)]
        [TestCase(CanonicalResultSetStatus.Deleting)]
        public async Task IsOriginalSearchResultSetStored_WhenResultSetIsNotComplete_ReturnsFalse(CanonicalResultSetStatus status)
        {
            var searchId = fixture.Create<string>();
            canonicalResultSetRepository.GetCanonicalResultSetSummary(searchId)
                .Returns(new CanonicalResultSet { Status = status });

            var isStored = await repeatSearchValidator.IsOriginalSearchResultSetStored(searchId);

            isStored.Should().BeFalse();
        }

        [Test]
        public async Task ValidateRepeatSearchRequest_WhenOriginalResultSetCannotBeMarkedUsed_ThrowsException()
        {
            // For example a set in progress or being deleted: the repository marks only a complete set.
            var searchId = fixture.Create<string>();

            var repeatSearch = new RepeatSearchRequest
            {
                SearchRequest = new SearchRequestBuilder().Build(),
                OriginalSearchId = searchId,
                SearchCutoffDate = DateTimeOffset.Now
            };

            canonicalResultSetRepository.MarkResultSetUsed(searchId).Returns(false);

            var act = () => repeatSearchValidator.ValidateRepeatSearchAndThrow(repeatSearch);

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}