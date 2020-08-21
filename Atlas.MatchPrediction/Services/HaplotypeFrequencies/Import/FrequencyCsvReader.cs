using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencyCsvReader
    {
        IEnumerable<HaplotypeFrequency> GetFrequencies(Stream stream);
    }

    internal class FrequencyCsvReader : IFrequencyCsvReader
    {
        public IEnumerable<HaplotypeFrequency> GetFrequencies(Stream stream)
        {
            var haplotypeFrequencies = ReadCsv(stream).ToList();

            if (!FrequencySetValidity(haplotypeFrequencies))
            {
                throw new MalformedHaplotypeFileException("The nomenclature version, population ID, registry code, and ethnicity code must be the same for each frequency");
            }

            return haplotypeFrequencies;
        }

        private static bool FrequencySetValidity(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            var distinctFrequencySetInfo = haplotypeFrequencies
                .Select(hf => new {hf.RegistryCode, hf.EthnicityCode, hf.HlaNomenclatureVersion, hf.PopulationId}).Distinct();

            return distinctFrequencySetInfo.Count() == 1;
        }

        public IEnumerable<HaplotypeFrequency> ReadCsv(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader))
            {
                ConfigureCsvReader(csv);
                while (TryRead(csv))
                {
                    HaplotypeFrequency haplotypeFrequency = null;

                    try
                    {
                        haplotypeFrequency = csv.GetRecord<HaplotypeFrequency>();
                    }
                    catch (CsvHelperException e)
                    {
                        throw new HaplotypeFormatException(e);
                    }

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

                    yield return haplotypeFrequency;
                }
            }
        }

        private static void ConfigureCsvReader(IReaderRow csvReader)
        {
            csvReader.Configuration.Delimiter = ";";
            csvReader.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");
            csvReader.Configuration.PrepareHeaderForMatch = (header, index) => header.ToUpper();
            csvReader.Configuration.RegisterClassMap<HaplotypeFrequencyMap>();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class HaplotypeFrequencyMap : ClassMap<HaplotypeFrequency>
        {
            public HaplotypeFrequencyMap()
            {
                Map(m => m.A);
                Map(m => m.B);
                Map(m => m.C);
                Map(m => m.Dqb1);
                Map(m => m.Drb1);
                Map(m => m.Frequency).Name("freq");
                Map(m => m.HlaNomenclatureVersion).Name("nomenclature_version");
                Map(m => m.PopulationId).Name("population_id");
                Map(m => m.RegistryCode).Name("don_pool");
                Map(m => m.EthnicityCode).Name("ethn");
            }
        }

        private static bool TryRead(CsvReader reader)
        {
            try
            {
                return reader.Read();
            }
            catch (BadDataException e)
            {
                throw new MalformedHaplotypeFileException($"Invalid CSV was encountered: {e.Message}");
            }
        }
    }
}
