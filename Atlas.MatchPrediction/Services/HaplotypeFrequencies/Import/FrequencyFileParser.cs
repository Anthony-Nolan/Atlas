using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.MatchPrediction.Models;
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
                var frequencySetFile = new FrequencySetFileSchema();

                while (TryRead(reader))
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        continue;
                    }

                    try
                    {
                        frequencySetFile = DeserializeProperty(frequencySetFile, serializer, reader, reader.Value?.ToString());
                    }
                    catch (JsonSerializationException e)
                    {
                        throw new HaplotypeFormatException(e);
                    }
                }

                return frequencySetFile;
            }
        }

        private static FrequencySetFileSchema DeserializeProperty(
            FrequencySetFileSchema frequencySetFile,
            JsonSerializer serializer,
            JsonReader reader,
            string propertyName)
        {
            switch (propertyName)
            {
                case "nomenclatureVersion":
                    TryRead(reader); // Read into property
                    frequencySetFile.NomenclatureVersion = serializer.Deserialize<string>(reader);
                    break;
                case "donPool":
                    TryRead(reader); // Read into property
                    frequencySetFile.RegistryCodes = serializer.Deserialize<string[]>(reader);
                    break;
                // ReSharper disable once StringLiteralTypo
                case "ethn":
                    TryRead(reader);
                    frequencySetFile.Ethnicity = serializer.Deserialize<string>(reader);
                    break;
                case "populationId":
                    TryRead(reader); // Read into property
                    frequencySetFile.PopulationId = serializer.Deserialize<int>(reader);
                    break;
                case "frequencies":
                    TryRead(reader); // Read into property
                    TryRead(reader); // Read into array
                    frequencySetFile.Frequencies = DeserializeFrequencies(serializer, reader).ToList();
                    break;
                default:
                    throw new MalformedHaplotypeFileException($"Unrecognised property: {propertyName} encountered in haplotype frequency file.");
            }

            return frequencySetFile;
        }

        private static IEnumerable<FrequencyRecord> DeserializeFrequencies(JsonSerializer serializer, JsonReader reader)
        {
            do
            {
                var frequencyRecord = serializer.Deserialize<FrequencyRecord>(reader);

                if (frequencyRecord == null)
                {
                    throw new MalformedHaplotypeFileException("Haplotype frequency in input file could not be parsed.");
                }

                if (frequencyRecord.Frequency == 0m)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency property frequency cannot be 0.");
                }

                if (frequencyRecord.A == null ||
                    frequencyRecord.B == null ||
                    frequencyRecord.C == null ||
                    frequencyRecord.Dqb1 == null ||
                    frequencyRecord.Drb1 == null)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency loci cannot be null.");
                }

                yield return frequencyRecord;
            } while (TryRead(reader) && reader.TokenType != JsonToken.EndArray);
        }

        private static bool TryRead(JsonReader reader)
        {
            try
            {
                return reader.Read();
            }
            catch (JsonException e)
            {
                throw new MalformedHaplotypeFileException($"Invalid JSON was encountered: {e.Message}");
            }
        }
    }
}
