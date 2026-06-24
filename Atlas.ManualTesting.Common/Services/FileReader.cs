using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Atlas.ManualTesting.Common.Services;

public interface IFileReader<T>
{
    Task<IReadOnlyCollection<T>> ReadAllLines(string delimiter, string filePath, bool hasHeaderRecord = true);
    IAsyncEnumerable<T> ReadAsync(string delimiter, string filePath);
}

public class FileReader<T> : IFileReader<T>
{
    public async Task<IReadOnlyCollection<T>> ReadAllLines(
        string delimiter,
        string filePath,
        bool hasHeaderRecord = true)
    {
        FileChecks(filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = hasHeaderRecord,
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

        return csv.GetRecords<T>().ToList();
    }

    public async IAsyncEnumerable<T> ReadAsync(string delimiter, string filePath)
    {
        FileChecks(filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

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