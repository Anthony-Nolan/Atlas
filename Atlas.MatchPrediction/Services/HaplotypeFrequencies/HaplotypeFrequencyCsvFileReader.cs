using System;
using Atlas.MatchPrediction.Data.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequenciesStreamReader
    {
        IEnumerable<HaplotypeFrequency> GetFrequencies(Stream stream, int batchSize, int startFrom);
    }

    public class HaplotypeFrequencyCsvFileReader : IHaplotypeFrequenciesStreamReader
    {
        public IEnumerable<HaplotypeFrequency> GetFrequencies(Stream stream, int batchSize, int startFrom)
        {
            if (stream == null)
            {
                throw new ArgumentNullException();
            }

            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than 0.");
            }

            return ReadHaplotypeFrequenciesBatch(stream, batchSize, startFrom);
        }

        private static IEnumerable<HaplotypeFrequency> ReadHaplotypeFrequenciesBatch(Stream stream, int batchSize, int startFrom)
        {
            var frequencies = new List<HaplotypeFrequency>();

            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader))
            {
                stream.Position = 0;
                ConfigureCsvReader(csv);

                if (!SkipThroughFileToStartingPoint(startFrom, csv))
                {
                    return new List<HaplotypeFrequency>();
                }

                for (var i = 0; i < batchSize; i++)
                {
                    if (!csv.Read())
                    {
                        break;
                    }

                    frequencies.Add(csv.GetRecord<HaplotypeFrequency>());
                }
            }

            return frequencies;
        }

        /// <returns>Returns false if requested starting point is past the end of file, else returns true.</returns>
        private static bool SkipThroughFileToStartingPoint(int startFrom, IReader csv)
        {
            csv.Read();
            csv.ReadHeader();

            for (var i = 0; i < startFrom; i++)
            {
                if (!csv.Read())
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureCsvReader(IReaderRow csvReader)
        {
            csvReader.Configuration.Delimiter = ";";
            csvReader.Configuration.PrepareHeaderForMatch = (header, index) => header.ToUpper();
            csvReader.Configuration.RegisterClassMap<HaplotypeFrequencyMap>();
        }

        private sealed class HaplotypeFrequencyMap : ClassMap<HaplotypeFrequency>
        {
            public HaplotypeFrequencyMap()
            {
                Map(m => m.A);
                Map(m => m.B);
                Map(m => m.C);
                Map(m => m.DQB1);
                Map(m => m.DRB1);
                Map(m => m.Frequency).Name("freq");
                Map(m => m.Id).Ignore();
                Map(m => m.Set).Ignore();
            }
        }
    }
}
