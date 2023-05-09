using CsvHelper;

namespace Atlas.ManualTesting.Common.SubjectImport
{
    public interface ISubjectInfoReader
    {
        Task<IReadOnlyCollection<ImportedSubject>> Read(string filePath);
    }

    public class SubjectInfoReader : ISubjectInfoReader
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
