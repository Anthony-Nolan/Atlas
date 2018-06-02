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

        public IEnumerable<TWmdaHlaTyping> GetWmdaHlaTypingsForPermittedLoci(IWmdaFileReader fileReader)
        {
            var fileContents = fileReader.GetFileContentsWithoutHeader(fileName);
            var data = ExtractWmdaHlaTypingsForPermittedLociFromFileContents(fileContents);

            return data;
        }

        private IEnumerable<TWmdaHlaTyping> ExtractWmdaHlaTypingsForPermittedLociFromFileContents(IEnumerable<string> wmdaFileContents)
        {
            var extractionQuery =
                from line in wmdaFileContents
                select MapLineOfFileToWmdaHlaTypingElseNull(line) into typing
                where typing != null && typing.IsPermittedLocusTyping()
                select typing;

            var extractedData = extractionQuery.ToArray();
            return extractedData;
        }

        protected abstract TWmdaHlaTyping MapLineOfFileToWmdaHlaTypingElseNull(string line);
    }
}
