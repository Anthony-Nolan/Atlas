using System.IO;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using CsvHelper;
using MoreLinq.Extensions;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencyJsonReader
    {
        HaplotypeFrequencyFileRecord GetFrequencies(Stream stream);
    }

    internal class FrequencyJsonReader : IFrequencyJsonReader
    {
        public HaplotypeFrequencyFileRecord GetFrequencies(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var haplotypeFrequencyFile = new HaplotypeFrequencyFileRecord();

                var serializer = new JsonSerializer();

                while (TryRead(reader))
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;

                    var propertyName = reader.Value?.ToString();
                    try
                    {
                        switch (propertyName)
                        {
                            case nameof(HaplotypeFrequencyFileRecord.RegistryCodes):
                                // Read into property
                                TryRead(reader);

                                haplotypeFrequencyFile.RegistryCodes = serializer.Deserialize<string[]>(reader);

                                break;
                            case nameof(HaplotypeFrequencyFileRecord.Ethnicity):
                                // Read into property
                                TryRead(reader);

                                var ethnicity = serializer.Deserialize<string>(reader);

                                if (!ethnicity.IsNullOrEmpty() && haplotypeFrequencyFile.RegistryCodes.IsNullOrEmpty())
                                {
                                    throw new MalformedHaplotypeFileException(
                                        $"Cannot import set: Ethnicity ('{ethnicity}') provided but no registry");
                                }

                                haplotypeFrequencyFile.Ethnicity = ethnicity;

                                break;
                            case nameof(HaplotypeFrequencyFileRecord.PopulationId):
                                // Read into property
                                TryRead(reader);

                                haplotypeFrequencyFile.PopulationId = serializer.Deserialize<int>(reader);

                                break;
                            case nameof(HaplotypeFrequencyFileRecord.NomenclatureVersion):
                                // Read into property
                                TryRead(reader);

                                var nomenclatureVersion = serializer.Deserialize<string>(reader);

                                if (nomenclatureVersion.IsNullOrEmpty())
                                {
                                    throw new MalformedHaplotypeFileException($"Nomenclature version must be set");
                                }

                                haplotypeFrequencyFile.NomenclatureVersion = nomenclatureVersion;

                                break;
                            case nameof(HaplotypeFrequencyFileRecord.Frequencies):
                                TryRead(reader); // Read into property
                                TryRead(reader); // Read into array. Do not deserialize to collection, as the collection can be very large and requires streaming.

                                // Loops through all donors in array
                                do
                                {
                                    var haplotypeFrequency = serializer.Deserialize<HaplotypeFrequencyRecord>(reader);

                                    if (haplotypeFrequency == null)
                                    {
                                        throw new MalformedHaplotypeFileException("Haplotype in input file could not be parsed.");
                                    }

                                    if (haplotypeFrequency.Frequency == 0m)
                                    {
                                        throw new MalformedHaplotypeFileException($"Haplotype property frequency cannot be 0.");
                                    }

                                    if (haplotypeFrequency.A == null ||
                                        haplotypeFrequency.B == null ||
                                        haplotypeFrequency.C == null ||
                                        haplotypeFrequency.Dqb1 == null ||
                                        haplotypeFrequency.Drb1 == null)
                                    {
                                        throw new MalformedHaplotypeFileException($"Haplotype loci cannot be null.");
                                    }

                                    haplotypeFrequencyFile.Frequencies.Append(haplotypeFrequency);

                                } while (TryRead(reader) && reader.TokenType != JsonToken.EndArray);

                                break;
                            default:
                                throw new MalformedHaplotypeFileException($"Unrecognised property: {propertyName} encountered in frequency import file.");
                        }
                    }
                    catch (JsonSerializationException e)
                    {
                        throw new HaplotypeFormatException(e);
                    }
                }
                return haplotypeFrequencyFile;
            }
        }

        private static bool TryRead(JsonReader reader)
        {
            try
            {
                return reader.Read();
            }
            catch (BadDataException e)
            {
                throw new MalformedHaplotypeFileException($"Invalid JSON was encountered: {e.Message}");
            }
        }
    }
}
