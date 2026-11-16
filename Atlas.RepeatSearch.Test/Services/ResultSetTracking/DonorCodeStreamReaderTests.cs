using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using AwesomeAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking
{
    [TestFixture]
    public class DonorCodeStreamReaderTests
    {
        [Test]
        public async Task ReadDonorCodes_ForSerialisedResultSet_ReturnsDonorCodesInOrder()
        {
            var resultSet = new OriginalMatchingAlgorithmResultSet
            {
                SearchRequestId = "search-id",
                Results = new[] { "donor-1", "donor-2", "donor-3" }.Select(BuildResult).ToList(),
                TotalResults = 3
            };

            var donorCodes = await ReadDonorCodes(JsonConvert.SerializeObject(resultSet));

            donorCodes.Should().Equal("donor-1", "donor-2", "donor-3");
        }

        [Test]
        public async Task ReadDonorCodes_SkipsNestedObjectsAndOtherProperties()
        {
            const string json = @"{
                ""SearchRequestId"": ""search-id"",
                ""Nested"": { ""Results"": [ { ""DonorCode"": ""not-a-result"" } ] },
                ""Results"": [
                    { ""ScoringResult"": { ""DonorCode"": ""nested"", ""Grades"": [1, 2, { ""a"": ""b"" }] }, ""DonorCode"": ""donor-1"", ""Date"": ""2026-11-16T00:00:00Z"" },
                    { ""DonorCode"": ""donor-2"", ""ExternalDonorCode"": ""other"" }
                ],
                ""TotalResults"": 2
            }";

            var donorCodes = await ReadDonorCodes(json);

            donorCodes.Should().Equal("donor-1", "donor-2");
        }

        [Test]
        public async Task ReadDonorCodes_WhenResultsAreNull_ReturnsNoCodes()
        {
            var donorCodes = await ReadDonorCodes(@"{ ""SearchRequestId"": ""search-id"", ""Results"": null }");

            donorCodes.Should().BeEmpty();
        }

        [Test]
        public async Task ReadDonorCodes_WhenThereIsNoResultsProperty_ReturnsNoCodes()
        {
            // Batched results: the results are in the batch files, not in the result set file.
            var donorCodes = await ReadDonorCodes(@"{ ""SearchRequestId"": ""search-id"", ""TotalResults"": 2 }");

            donorCodes.Should().BeEmpty();
        }

        [Test]
        public async Task ReadDonorCodes_ForEmptyResults_ReturnsNoCodes()
        {
            var donorCodes = await ReadDonorCodes(@"{ ""Results"": [] }");

            donorCodes.Should().BeEmpty();
        }

        [Test]
        public async Task ReadDonorCodes_WhenFileIsNotAJsonObject_Throws()
        {
            await FluentActions.Invoking(() => ReadDonorCodes("[]")).Should().ThrowAsync<JsonReaderException>();
        }

        private static MatchingAlgorithmResult BuildResult(string donorCode) => new()
        {
            DonorCode = donorCode,
            MatchingResult = new MatchingResult { TotalMatchCount = 10 },
            ScoringResult = new ScoringResult { GradeScore = 5, ConfidenceScore = 3 }
        };

        private static async Task<List<string>> ReadDonorCodes(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var donorCodes = new List<string>();
            await foreach (var donorCode in DonorCodeStreamReader.ReadDonorCodes(stream))
            {
                donorCodes.Add(donorCode);
            }

            return donorCodes;
        }
    }
}
