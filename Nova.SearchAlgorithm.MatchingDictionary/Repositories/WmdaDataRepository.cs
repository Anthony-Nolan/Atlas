using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IWmdaDataRepository
    {
        IEnumerable<HlaNom> HlaNomSerologies { get; }
        IEnumerable<HlaNom> HlaNomAlleles { get; }
        IEnumerable<HlaNomP> HlaNomP { get; }
        IEnumerable<HlaNomG> HlaNomG { get; }
        IEnumerable<RelSerSer> RelSerSer { get; }
        IEnumerable<RelDnaSer> RelDnaSer { get; }
        IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; }
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        public IEnumerable<HlaNom> HlaNomSerologies { get; private set; }
        public IEnumerable<HlaNom> HlaNomAlleles { get; private set; }
        public IEnumerable<HlaNomP> HlaNomP { get; private set; }
        public IEnumerable<HlaNomG> HlaNomG { get; private set; }
        public IEnumerable<RelSerSer> RelSerSer { get; private set; }
        public IEnumerable<RelDnaSer> RelDnaSer { get; private set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; private set; }

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
            PopulateWmdaDataCollections();
        }

        private void PopulateWmdaDataCollections()
        {
            HlaNomSerologies = GetWmdaData<HlaNom>(nameof(HlaNomSerologies));
            HlaNomAlleles = GetWmdaData<HlaNom>(nameof(HlaNomAlleles));
            HlaNomP = GetWmdaData<HlaNomP>(nameof(HlaNomP));
            HlaNomG = GetWmdaData<HlaNomG>(nameof(HlaNomG));
            RelSerSer = GetWmdaData<RelSerSer>(nameof(RelSerSer));
            RelDnaSer = GetWmdaData<RelDnaSer>(nameof(RelDnaSer));
            ConfidentialAlleles = GetWmdaData<ConfidentialAllele>(nameof(ConfidentialAlleles));
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(string wmdaDataName)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            var toolSet = WmdaDataExtraction.GetWmdaDataExtractionToolSet(wmdaDataName);
            var fileContents = wmdaFileReader.GetFileContentsWithoutHeader(toolSet.FileName);
            var data = ExtractWmdaDataFromFileContents<TWmdaHlaTyping>(fileContents, toolSet);

            return data;
        }

        private static TWmdaHlaTyping[] ExtractWmdaDataFromFileContents<TWmdaHlaTyping>(
            IEnumerable<string> wmdaFileContents,
            WmdaDataExtractionToolSet toolSet)
                where TWmdaHlaTyping : IWmdaHlaTyping
        {
            var regex = new Regex(toolSet.RegexPattern);

            var extractionQuery =
                from line in wmdaFileContents
                select regex.Match(line).Groups into regexResults
                where regexResults.Count > 0
                select toolSet.DataMapper.MapDataExtractedFromWmdaFile(regexResults) into mapped
                where toolSet.DataFilter(mapped)
                select (TWmdaHlaTyping) mapped;

            var enumeratedData = extractionQuery.ToArray();

            return enumeratedData;
        }
    }
}
