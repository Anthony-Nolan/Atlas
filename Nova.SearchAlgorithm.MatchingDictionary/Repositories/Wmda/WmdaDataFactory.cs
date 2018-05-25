using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    public static class WmdaDataFactory 
    {
        private class WmdaDataExtractionToolSet
        {
            public string RegexPattern { get; }
            public IWmdaDataMapper<IWmdaHlaTyping> WmdaDataMapper { get; }

            public WmdaDataExtractionToolSet(string regexPattern, IWmdaDataMapper<IWmdaHlaTyping> wmdaDataMapper)
            {
                RegexPattern = regexPattern;
                WmdaDataMapper = wmdaDataMapper;
            }
        }

        private static readonly Dictionary<string, WmdaDataExtractionToolSet> ExtractionToolSets = new Dictionary<string, WmdaDataExtractionToolSet>
        {
            {nameof(HlaNom), new WmdaDataExtractionToolSet(@"^(\w+\*{0,1})\;([\w:]+)\;\d+\;(\d*)\;([\w:]*)\;", new HlaNomMapper()) },
            {nameof(HlaNomP), new WmdaDataExtractionToolSet(@"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$", new HlaNomPMapper()) },
            {nameof(HlaNomG), new WmdaDataExtractionToolSet(@"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$", new HlaNomGMapper()) },
            {nameof(RelSerSer), new WmdaDataExtractionToolSet(@"(\w+)\;(\d*)\;([\d\/]*)\;([\d\/]*)", new RelSerSerMapper()) },
            {nameof(RelDnaSer), new WmdaDataExtractionToolSet(@"^(\w+\*)\;([\w:]+)\;([\d\/\\?]*);([\d\/\\?]*)\;([\d\/\\?]*)\;([\d\/\\?]*)$", new RelDnaSerMapper()) },
            {nameof(ConfidentialAllele), new WmdaDataExtractionToolSet(@"^Confidential,(\w+\*)([\w:]+),", new ConfidentialAlleleMapper()) }
        };

        public static IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(IEnumerable<string> wmdaFileContents, Func<IWmdaHlaTyping, bool> filter)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            var typeName = typeof(TWmdaHlaTyping).Name;
            ExtractionToolSets.TryGetValue(typeName, out var extractionTool);

            if (extractionTool == null)
                throw new ArgumentException($"Method cannot handle type of {typeName}.");

            var regex = new Regex(extractionTool.RegexPattern);

            var dataExtractionQuery =
                from line in wmdaFileContents
                select regex.Match(line).Groups into regexResults
                where regexResults.Count > 0
                select extractionTool.WmdaDataMapper.MapDataExtractedFromWmdaFile(regexResults) into mapped
                where filter(mapped)
                select (TWmdaHlaTyping)mapped;
            
            var data = dataExtractionQuery.ToArray();
            return data;
        }
    }
}
