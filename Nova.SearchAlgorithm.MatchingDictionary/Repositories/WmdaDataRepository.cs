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
        IEnumerable<HlaNom> Serologies { get; }
        IEnumerable<HlaNom> Alleles { get; }
        IEnumerable<HlaNomP> PGroups { get; }
        IEnumerable<HlaNomG> GGroups { get; }
        IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; }
        IEnumerable<RelDnaSer> DnaToSerologyRelationships { get; }
        IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; }
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        public IEnumerable<HlaNom> Serologies { get; private set; }
        public IEnumerable<HlaNom> Alleles { get; private set; }
        public IEnumerable<HlaNomP> PGroups { get; private set; }
        public IEnumerable<HlaNomG> GGroups { get; private set; }
        public IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; private set; }
        public IEnumerable<RelDnaSer> DnaToSerologyRelationships { get; private set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; private set; }

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
            PopulateWmdaDataCollections();
        }

        private void PopulateWmdaDataCollections()
        {
            Serologies = GetWmdaData<HlaNom>(nameof(Serologies));
            Alleles = GetWmdaData<HlaNom>(nameof(Alleles));
            PGroups = GetWmdaData<HlaNomP>(nameof(PGroups));
            GGroups = GetWmdaData<HlaNomG>(nameof(GGroups));
            SerologyToSerologyRelationships = GetWmdaData<RelSerSer>(nameof(SerologyToSerologyRelationships));
            DnaToSerologyRelationships = GetWmdaData<RelDnaSer>(nameof(DnaToSerologyRelationships));
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
