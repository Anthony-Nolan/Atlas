using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    public static class WmdaDataFactory
    {
        public static IEnumerable<TReturn> GetData<TReturn>(IWmdaRepository repo, Func<IWmdaHlaTyping, bool> filter)
            where TReturn : IWmdaHlaTyping
        {
            IWmdaDataExtractor dataExtractor;

            switch (new List<TReturn>())
            {
                case List<HlaNom> _:
                    dataExtractor = new HlaNomExtractor();
                    break;
                case List<HlaNomP> _:
                    dataExtractor = new HlaNomPExtractor();
                    break;
                case List<RelSerSer> _:
                    dataExtractor = new RelSerSerExtractor();
                    break;
                case List<RelDnaSer> _:
                    dataExtractor = new RelDnaSerExtractor();
                    break;
                case List<Confidential> _:
                    dataExtractor = new ConfidentialExtractor();
                    break;
                default:
                    return new List<TReturn>();
            }

            return dataExtractor.ExtractData(repo).Where(filter).Select(w => (TReturn)w);
        }
    }
}
