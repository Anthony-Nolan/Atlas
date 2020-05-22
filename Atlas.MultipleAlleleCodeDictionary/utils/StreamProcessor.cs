using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface IStreamProcessor
    {
        Stream DownloadAndUnzipStream();
    }

    public class StreamProcessor : IStreamProcessor
    {
        private readonly WebClient webClient = new WebClient();
        private readonly string url;

        public StreamProcessor(string url)
        {
            this.url = url;
        }

        public Stream DownloadAndUnzipStream()
        {
            var stream = DownloadStream();
            var unzippedStream = UnzipStream(stream);
            return unzippedStream;
        }

        private Stream DownloadStream()
        {
            var stream = new MemoryStream(webClient.DownloadData(url));
            return stream;
        }

        private static Stream UnzipStream(Stream stream)
        {
            var zipArchive = new ZipArchive(stream);
            var fileName = zipArchive.Entries.Select(e => e.FullName).Single();
            var entry = zipArchive.GetEntry(fileName);
            var unzippedStream = entry?.Open();
            return unzippedStream;
        }
    }
}