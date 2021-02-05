using System;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Models
{
    [TestFixture]
    public class SearchableDonorUpdateTests
    {
        /// <summary>
        /// Regression test to cover a bug where the PublishDateTime was not being set correctly on deserialisation.
        /// </summary>
        [Test]
        public void New_ValueProvidedInMessage_SetsPublishedDateTimeToMessageValueOnDeserialisation()
        {
            var dateTime = new DateTimeOffset(2020, 11, 11, 11, 11, 11, TimeSpan.Zero);
            var message = @$"{{ ""PublishedDateTime"": ""{dateTime}"" }}";

            var update = JsonConvert.DeserializeObject<SearchableDonorUpdate>(message);
            update.PublishedDateTime.Should().Be(dateTime);
        }

        /// <summary>
        /// Regression test to cover a bug where the PublishDateTime was not being set correctly on deserialisation.
        /// </summary>
        [Test]
        public void New_NoValueProvidedInMessage_SetsPublishedDateTimeToUtcNowOnDeserialisation()
        {
            const string message = @"{ }";
            var update = JsonConvert.DeserializeObject<SearchableDonorUpdate>(message);
            update.PublishedDateTime.Should().BeCloseTo(DateTimeOffset.UtcNow);
        }

        [Test]
        public void New_SetsPublishedDateTimeToUtcNowByDefault()
        {
            var update = new SearchableDonorUpdate();
            update.PublishedDateTime.Should().BeCloseTo(DateTimeOffset.UtcNow);
        }
    }
}
