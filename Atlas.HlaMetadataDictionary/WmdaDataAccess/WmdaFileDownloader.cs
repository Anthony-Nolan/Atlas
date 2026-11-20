using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;

namespace Atlas.HlaMetadataDictionary.WmdaDataAccess;

internal class WmdaFileDownloader : IWmdaFileReader
{
    /// <summary>The name of the <see cref="HttpClient"/> that downloads the WMDA files.</summary>
    internal const string HttpClientName = nameof(WmdaFileDownloader);

    /// <summary>
    /// The <see cref="HttpClient.Timeout"/> of the client: the time limit of a whole download, the content included.
    /// </summary>
    internal static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory httpClientFactory;
    private readonly string hlaNomenclatureSourceUrl;

    public WmdaFileDownloader(IHttpClientFactory httpClientFactory, HlaMetadataDictionarySettings settings)
    {
        this.httpClientFactory = httpClientFactory;
        hlaNomenclatureSourceUrl = settings.HlaNomenclatureSourceUrl;
    }

    public IEnumerable<string> GetFileContentsWithoutHeader(string hlaNomenclatureVersion, string fileName)
    {
        using var response = Download(GetFileAddress(hlaNomenclatureVersion, fileName));
        using var reader = new StreamReader(response.Content.ReadAsStream());

        return reader.ReadToEnd()
            .Split('\n')
            .SkipWhile(IsCommentLine);
    }

    public string GetFirstNonCommentLine(string nomenclatureVersion, string fileName)
    {
        var fileAddress = GetFileAddress(nomenclatureVersion, fileName);
        using var response = Download(fileAddress);
        using var reader = new StreamReader(response.Content.ReadAsStream());

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (IsCommentLine(line))
            {
                continue;
            }

            return line;
        }

        throw new Exception($"No non comment lines found when reading: {fileAddress}");
    }

    /// <summary>
    /// Returns the response with its whole content already read into memory, also when the caller needs only the first
    /// lines. The client's timeout then covers the content too: with <see cref="HttpCompletionOption.ResponseHeadersRead"/>,
    /// it would end when the headers arrive, and a read of the content that stalls would never end.
    /// </summary>
    private HttpResponseMessage Download(string fileAddress)
    {
        // A new client for each download, not one kept in a field: the dictionary that holds this reader stays in the
        // persistent cache much longer than the handler lifetime of the factory, and a client kept that long does not
        // follow DNS changes.
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, fileAddress);
        var response = httpClient.Send(request, HttpCompletionOption.ResponseContentRead);

        // Unlike WebClient, HttpClient does not throw on a non-success status. Without this check, the error page of a
        // missing file would be parsed as its content.
        try
        {
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (HttpRequestException exception)
        {
            response.Dispose();
            throw new HttpRequestException(
                $"Could not download the WMDA file {fileAddress}. {exception.Message}",
                exception,
                exception.StatusCode);
        }
    }

    private string GetFileAddress(string hlaNomenclatureVersion, string fileName)
    {
        return $"{hlaNomenclatureSourceUrl}{hlaNomenclatureVersion}/{fileName}";
    }

    private static bool IsCommentLine(string line)
    {
        return line.StartsWith("#");
    }
}
