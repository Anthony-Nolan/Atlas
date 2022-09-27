using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Validation.Models;
using CsvHelper;

namespace Atlas.MatchPrediction.Test.Validation.Services
{
    internal interface ISubjectInfoReader
    {
        Task<IReadOnlyCollection<ImportedSubject>> Read(string filePath);
    }

    internal class SubjectInfoReader : ISubjectInfoReader
    {
        public async Task<IReadOnlyCollection<ImportedSubject>> Read(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"File not found at {filePath}.");
            }
            
            await using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = ";";
                    csv.Configuration.HeaderValidated = null;
                    csv.Configuration.MissingFieldFound = null;
                    csv.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");
                    return csv.GetRecords<ImportedSubject>().ToList();
                }
            }
        }
    }
}
