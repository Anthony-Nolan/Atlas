using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;

namespace Atlas.HlaMetadataDictionary.WmdaDataAccess
{
    internal class WmdaFileDownloader : IWmdaFileReader
    {
        private readonly string hlaNomenclatureSourceUrl;

        public WmdaFileDownloader(HlaMetadataDictionarySettings settings)
        {
            hlaNomenclatureSourceUrl = settings.HlaNomenclatureSourceUrl;
        }

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaNomenclatureVersion, string fileName)
        {
            return new WebClient()
                .DownloadString(GetFileAddress(hlaNomenclatureVersion, fileName))
                .Split('\n')
                .SkipWhile(IsCommentLine);
        }

        public string GetFirstNonCommentLine(string nomenclatureVersion, string fileName)
        {
            var fileAddress = GetFileAddress(nomenclatureVersion, fileName);
            var stream = new WebClient().OpenRead(fileAddress);
            if (stream == null)
            {
                throw new Exception($"Null stream returned from WebClient when reading from: {fileAddress}");
            }

            using (var reader = new StreamReader(stream))
            {
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
}