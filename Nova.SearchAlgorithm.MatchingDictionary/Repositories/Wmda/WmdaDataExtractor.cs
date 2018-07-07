using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal abstract class WmdaDataExtractor<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected const string WmdaFilePathPrefix = "wmda/";

        private readonly string fileName;

        protected WmdaDataExtractor(string fileName)
        {
            this.fileName = fileName;
        }

        public IEnumerable<TWmdaHlaTyping> GetWmdaHlaTypingsForPermittedLoci(IWmdaFileReader fileReader, string hlaDatabaseVersion)
        {
            var fileContents = fileReader.GetFileContentsWithoutHeader(hlaDatabaseVersion, fileName);
            var data = ExtractWmdaHlaTypingsForPermittedLociFromFileContents(fileContents);

            return data;
        }

        private IEnumerable<TWmdaHlaTyping> ExtractWmdaHlaTypingsForPermittedLociFromFileContents(IEnumerable<string> wmdaFileContents)
        {
            return 
                wmdaFileContents
                .Select(MapLineOfFileContentsToWmdaHlaTypingElseNull)
                .Where(typing => typing != null && typing.IsPermittedLocusTyping());
        }

        protected abstract TWmdaHlaTyping MapLineOfFileContentsToWmdaHlaTypingElseNull(string line);
    }
}
