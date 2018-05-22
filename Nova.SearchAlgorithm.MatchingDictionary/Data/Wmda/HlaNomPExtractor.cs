using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    internal class HlaNomPExtractor : IWmdaDataExtractor
    {
       public IEnumerable<IWmdaHlaTyping> ExtractData(IWmdaRepository repo)
        {
            var data = new List<HlaNomP>();

            foreach (var line in repo.HlaNomP)
            {
                var matched = new Regex(@"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$").Match(line).Groups;
                data.Add(new HlaNomP(
                    matched[1].Value,
                    matched[3].Value.Equals("") ? matched[2].Value : matched[3].Value,
                    matched[2].Value.Split('/')
                ));
            }

            return data;
        }
    }
}
