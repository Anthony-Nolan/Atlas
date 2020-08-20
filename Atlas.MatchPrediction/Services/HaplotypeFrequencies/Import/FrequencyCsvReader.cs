using System.Collections.Generic;
using System.IO;
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
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader))
            {
                ConfigureCsvReader(csv);
                while (TryRead(csv))
                {
                    HaplotypeFrequency haplotypeFrequencyFile = null;

                    try
                    {
                        haplotypeFrequencyFile = csv.GetRecord<HaplotypeFrequency>();
                    }
                    catch (CsvHelperException e)
                    {
                        throw new HaplotypeFormatException(e);
                    }

                    if (haplotypeFrequencyFile == null)
                    {
                        throw new MalformedHaplotypeFileException("Haplotype in input file could not be parsed.");
                    }

                    if (haplotypeFrequencyFile.Frequency == 0m)
                    {
                        throw new MalformedHaplotypeFileException($"Haplotype property frequency cannot be 0.");
                    }

                    if (haplotypeFrequencyFile.A == null ||
                        haplotypeFrequencyFile.B == null ||
                        haplotypeFrequencyFile.C == null ||
                        haplotypeFrequencyFile.Dqb1 == null ||
                        haplotypeFrequencyFile.Drb1 == null)
                    {
                        throw new MalformedHaplotypeFileException($"Haplotype loci cannot be null.");
                    }

                    yield return haplotypeFrequencyFile;
                }
            }
        }

        private static void ConfigureCsvReader(IReaderRow csvReader)
        {
            csvReader.Configuration.Delimiter = ";";
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
