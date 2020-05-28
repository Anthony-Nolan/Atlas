using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface IStreamProcessor
    {
        public Stream DownloadAndUnzipStream();
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
            var stream = DownloadToMemoryStream();
            return UnzipStream(stream);
        }

        private Stream DownloadToMemoryStream()
        {
            var data = webClient.DownloadDataTaskAsync(url).Result;
            var stream = new MemoryStream(data);
            return stream;
        }

        private static Stream UnzipStream(Stream stream)
        {
            var zipArchive = new ZipArchive(stream);
            if (zipArchive.Entries.Count > 1)
            {
                throw new InvalidOperationException("NMDP zip archive contained more than one file");
            }

            var fileName = zipArchive.Entries.Single().FullName;
            var entry = zipArchive.GetEntry(fileName);
            var unzippedStream = entry?.Open();
            return unzippedStream;
        }
    }
}