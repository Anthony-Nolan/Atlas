using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencySetImportService
    {
        Task Import(Stream blob, HaplotypeFrequencySetMetaData metaData);
    }

    public class HaplotypeFrequencySetImportService : IHaplotypeFrequencySetImportService
    {
        private readonly IHaplotypeFrequencySetImportRepository importRepository;

        public HaplotypeFrequencySetImportService(IHaplotypeFrequencySetImportRepository importRepository)
        {
            this.importRepository = importRepository;
        }

        public async Task Import(Stream blob, HaplotypeFrequencySetMetaData metaData)
        {
            var setId = await importRepository.AddHaplotypeFrequencySet(
                new HaplotypeFrequencySet
                {
                    Registry = metaData.Registry,
                    Ethnicity = metaData.Ethnicity,
                    Active = true,
                    Name = metaData.Name,
                    DateTimeAdded = DateTimeOffset.Now
                });

            var frequencies = ReadAllHaplotypeFrequencies(blob);

            await importRepository.AddHaplotypeFrequencies(setId, frequencies);
        }

        private static IEnumerable<HaplotypeFrequency> ReadAllHaplotypeFrequencies(Stream blob)
        {
            using var reader = new StreamReader(blob);
            using var csv = new CsvReader(reader);
            csv.Configuration.Delimiter = ";";
            csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToUpper();
            csv.Configuration.RegisterClassMap<HaplotypeFrequencyMap>();
            return csv.GetRecords<HaplotypeFrequency>().ToList();
        }

        public sealed class HaplotypeFrequencyMap : ClassMap<HaplotypeFrequency>
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
