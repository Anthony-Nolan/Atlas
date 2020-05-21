using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencySetImportService
    {
        Task Import(HaplotypeFrequencySetMetaData metaData, Stream blob);
    }

    public class HaplotypeFrequencySetImportService : IHaplotypeFrequencySetImportService
    {
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;

        public HaplotypeFrequencySetImportService(IHaplotypeFrequencySetRepository setRepository, IHaplotypeFrequenciesRepository frequenciesRepository)
        {
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
        }

        public async Task Import(HaplotypeFrequencySetMetaData metaData, Stream blob)
        {
            if (metaData == null || blob == null)
            {
                throw new ArgumentNullException();
            }

            var set = await AddSet(metaData);

            var frequencies = ReadAllHaplotypeFrequencies(blob);

            await frequenciesRepository.AddHaplotypeFrequencies(set.Id, frequencies);
        }

        private async Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySetMetaData metaData)
        {
            ValidateMetaData(metaData);
            await DeactivateActiveSetIfExists(metaData);
            return await AddNewActiveSet(metaData);
        }

        private static void ValidateMetaData(HaplotypeFrequencySetMetaData metaData)
        {
            if (!metaData.Ethnicity.IsNullOrEmpty() && metaData.Registry.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metaData.Ethnicity}') provided but no registry.");
            }
        }

        private async Task DeactivateActiveSetIfExists(HaplotypeFrequencySetMetaData metaData)
        {
           var existingSet = await setRepository.GetActiveSet(metaData.Registry, metaData.Ethnicity);

           if (existingSet == null)
           {
               return;
           }

           await setRepository.DeactivateSet(existingSet);
        }

        private async Task<HaplotypeFrequencySet> AddNewActiveSet(HaplotypeFrequencySetMetaData metaData)
        {
            var newSet = new HaplotypeFrequencySet
            {
                Registry = metaData.Registry,
                Ethnicity = metaData.Ethnicity,
                Active = true,
                Name = metaData.Name,
                DateTimeAdded = DateTimeOffset.Now
            };

            return await setRepository.AddSet(newSet);
        }

        private static IEnumerable<HaplotypeFrequency> ReadAllHaplotypeFrequencies(Stream blob)
        {
            // TODO: ATLAS-15 - Complete implementation
            return new List<HaplotypeFrequency>();
            /*
            using var reader = new StreamReader(blob);
            using var csv = new CsvReader(reader);
            csv.Configuration.Delimiter = ";";
            csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToUpper();
            csv.Configuration.RegisterClassMap<HaplotypeFrequencyMap>();
            return csv.GetRecords<HaplotypeFrequency>().ToList();
            */
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
