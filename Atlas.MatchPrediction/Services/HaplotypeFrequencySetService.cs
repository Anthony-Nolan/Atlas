using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using CsvHelper;
using CsvHelper.Configuration;

namespace Atlas.MatchPrediction.Services
{
    public interface IHaplotypeFrequencySetService
    {
        Task ImportHaplotypeFrequencySet(string fileName, Stream blob);
    }

    public class HaplotypeFrequencySetService : IHaplotypeFrequencySetService
    {
        private readonly IHaplotypeFrequencySetImportRepository haplotypeFrequencySetImportRepository;

        public HaplotypeFrequencySetService(
            IHaplotypeFrequencySetImportRepository haplotypeFrequencySetImportRepository)
        {
            this.haplotypeFrequencySetImportRepository = haplotypeFrequencySetImportRepository;
        }

        public async Task ImportHaplotypeFrequencySet(string fileName, Stream blob)
        {
            await haplotypeFrequencySetImportRepository.InsertHaplotypeFrequencySet(SplitFileName(fileName),
                ReadAllHaplotypeFrequencies(blob));
        }

        public HaplotypeFrequencySet SplitFileName(string fileName)
        {
            var strArr = fileName.Split('/');
            Array.Resize(ref strArr, strArr.Length - 1);

            return new HaplotypeFrequencySet()
            {
                Registry = strArr.Length > 0 ? strArr[0] : null,
                Ethnicity = strArr.Length == 2 ? strArr[1] : null,
                Active = true
            };
        }

        public IEnumerable<HaplotypeFrequency> ReadAllHaplotypeFrequencies(Stream blob)
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
                Map(m => m.SetId).Ignore();
            }
        }
    }
}
