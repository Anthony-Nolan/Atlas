using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class WmdaDataExtractionToolSet
    {
        public string FileName { get; }
        public Func<IWmdaHlaTyping, bool> DataFilter { get; }
        public string RegexPattern { get; }
        public IWmdaDataMapper<IWmdaHlaTyping> DataMapper { get; }

        public WmdaDataExtractionToolSet(
            string fileName, 
            Func<IWmdaHlaTyping,bool> dataFilter, 
            string regexPattern, 
            IWmdaDataMapper<IWmdaHlaTyping> dataMapper)
        {
            FileName = fileName;
            DataFilter = dataFilter;
            RegexPattern = regexPattern;
            DataMapper = dataMapper;
        }
    }       
}
