using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Test.TestHelpers
{
    /// <summary>
    /// Helpers for constructing minimally-valid requests (i.e. ones that pass the API's request validators) plus the
    /// <see cref="HttpRequest"/> plumbing the functions read them from, so tests can focus on the resolution logic.
    /// </summary>
    internal static class RequestBuilders
    {
        public static SearchRequest ValidSearchRequest(bool? parallelMatchPrediction = null) => new()
        {
            SearchDonorType = DonorType.Adult,
            MatchCriteria = new MismatchCriteria
            {
                DonorMismatchCount = 0,
                IncludeBetterMatches = true,
                LocusMismatchCriteria = new LociInfoTransfer<int?> {A = 0, B = 0, Drb1 = 0}
            },
            ScoringCriteria = new ScoringCriteria
            {
                LociToScore = new List<Locus>(),
                LociToExcludeFromAggregateScore = new List<Locus>()
            },
            SearchHlaData = new PhenotypeInfoTransfer<string>
            {
                A = new LocusInfoTransfer<string> {Position1 = "*01:01", Position2 = "*01:01"},
                B = new LocusInfoTransfer<string> {Position1 = "*08:01", Position2 = "*08:01"},
                Drb1 = new LocusInfoTransfer<string> {Position1 = "*03:01", Position2 = "*03:01"}
            },
            ParallelMatchPrediction = parallelMatchPrediction
        };

        public static RepeatSearchRequest ValidRepeatSearchRequest(bool? parallelMatchPrediction = null) => new()
        {
            SearchRequest = ValidSearchRequest(parallelMatchPrediction),
            OriginalSearchId = Guid.NewGuid().ToString(),
            SearchCutoffDate = new DateTimeOffset(2026, 07, 31, 0, 0, 0, TimeSpan.Zero)
        };

        public static HttpRequest ToHttpRequest(this object body)
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
            return context.Request;
        }
    }
}
