using System;
using System.Collections.Generic;
using Atlas.MatchPrediction.Models;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Models
{
    // ReSharper disable InconsistentNaming

    /// <summary>
    /// File schema used to add attributes only needed for serialisation in tests
    /// </summary>
    internal class SerialisableFrequencySetFileContents : IDisposable
    {
        [JsonProperty(Order = 1)]
        public string nomenclatureVersion { get; set; }

        [JsonProperty(Order = 2)]
        public string[] donPool { get; set; }

        [JsonProperty(Order = 3)]
        public string ethn { get; set; }

        [JsonProperty(Order = 4)]
        public int populationId { get; set; }

        [JsonProperty(Order = 5)]
        public IEnumerable<FrequencyRecord> frequencies { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
