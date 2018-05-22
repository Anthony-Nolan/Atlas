using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    internal class HlaNomExtractor : IWmdaDataExtractor
    {
       public IEnumerable<IWmdaHlaType> ExtractData(IWmdaRepository repo)
        {
            var data = new List<HlaNom>();

            foreach (var line in repo.HlaNom)
            {
                var matched = new Regex(@"^(\w+\*{0,1})\;([\w:]+)\;\d+\;(\d*)\;([\w:]*)\;").Match(line).Groups;
                data.Add(new HlaNom(
                    matched[1].Value,
                    matched[2].Value,
                    !matched[3].Value.Equals(""),
                    matched[4].Value));
            }

            return data;
        }
    }
}
