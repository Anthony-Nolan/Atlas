using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors
{
    internal abstract class WmdaDataExtractor<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected const string WmdaFilePathPrefix = "wmda/";

        private readonly string fileName;

        protected WmdaDataExtractor(string fileName)
        {
            this.fileName = fileName;
        }

        public IEnumerable<TWmdaHlaTyping> GetWmdaHlaTypingsForHlaMetadataDictionaryLoci(IWmdaFileReader fileReader, string hlaNomenclatureVersion)
        {
            var fileContents = fileReader.GetFileContentsWithoutHeader(hlaNomenclatureVersion, fileName).ToList();
            ExtractHeaders(fileContents.First());
            return ExtractWmdaHlaTypingsForHlaMetadataDictionaryLoci(fileContents);
        }

        /// <returns>
        /// The information contained in the line, mapped to the appropriate type
        /// Returns null if a line cannot be parsed
        /// </returns>
        protected abstract TWmdaHlaTyping MapLineOfFileContentsToWmdaHlaTyping(string line);

        /// <summary>
        /// In some cases, the header information from the file is necessary to parse the remaining lines correctly
        /// </summary>
        protected virtual void ExtractHeaders(string headersLine)
        {
            // Do nothing by default
        }

        private IEnumerable<TWmdaHlaTyping> ExtractWmdaHlaTypingsForHlaMetadataDictionaryLoci(IEnumerable<string> wmdaFileContents)
        {
            return 
                wmdaFileContents
                    .Select(line => line.Trim())
                    .Select(MapLineOfFileContentsToWmdaHlaTyping)
                    .Where(typing => typing != null && typing.IsHlaMetadataDictionaryLocusTyping());
        }
    }
}
