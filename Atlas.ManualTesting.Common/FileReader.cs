using CsvHelper;

namespace Atlas.ManualTesting.Common
{
    public interface IFileReader<T>
    {
        Task<IReadOnlyCollection<T>> ReadAllLines(string delimiter, string filePath);
        IAsyncEnumerable<T> ReadAsync(string delimiter, string filePath);
    }

    public class FileReader<T> : IFileReader<T>
    {
        public async Task<IReadOnlyCollection<T>> ReadAllLines(string delimiter, string filePath)
        {
            FileChecks(filePath);

            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader);

            csv.Configuration.Delimiter = delimiter;
            csv.Configuration.HeaderValidated = null;
            csv.Configuration.MissingFieldFound = null;
            csv.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

            return csv.GetRecords<T>().ToList();
        }

        public async IAsyncEnumerable<T> ReadAsync(string delimiter, string filePath)
        {
            FileChecks(filePath);

            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader);

            csv.Configuration.Delimiter = delimiter;
            csv.Configuration.HeaderValidated = null;
            csv.Configuration.MissingFieldFound = null;
            csv.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                yield return csv.GetRecord<T>();
            }
        }

        private static void FileChecks(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"File not found at {filePath}.");
            }
        }
    }
}