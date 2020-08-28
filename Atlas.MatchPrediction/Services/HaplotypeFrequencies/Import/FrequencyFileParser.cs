using System.IO;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencyFileParser
    {
        FrequencySetFileSchema GetFrequencies(Stream stream);
    }

    internal class FrequencyFileParser : IFrequencyFileParser
    {
        public FrequencySetFileSchema GetFrequencies(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();

                FrequencySetFileSchema frequencySetFile;

                try
                {
                    frequencySetFile = serializer.Deserialize<FrequencySetFileSchema>(reader);
                }
                catch (JsonSerializationException e)
                {
                    throw new HaplotypeFormatException(e);
                }

                return frequencySetFile;
            }
        }
    }
}
