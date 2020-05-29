using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Settings.MacImport;
using Microsoft.Extensions.Options;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface IMacCodeDownloader
    {
        public Stream DownloadAndUnzipStream();
    }

    public class MacCodeDownloader : IMacCodeDownloader
    {
        private readonly WebClient webClient = new WebClient();
        private readonly string url;

        public MacCodeDownloader(IOptions<MacImportSettings> macImportSettings)
        {
            this.url = macImportSettings.Value.MacSourceUrl;
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