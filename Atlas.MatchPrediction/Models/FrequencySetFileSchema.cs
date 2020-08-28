using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Models
{
    public class FrequencySetFileSchema
    {
        public string NomenclatureVersion { get; set; }

        public string[] RegistryCodes { get; set; }

        public string Ethnicity { get; set; }

        public int PopulationId { get; set; }

        public IEnumerable<FrequencyRecord> Frequencies { get; set; }
    }
}
