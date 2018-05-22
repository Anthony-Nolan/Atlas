using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    internal class ConfidentialExtractor : IWmdaDataExtractor
    {
       public IEnumerable<IWmdaHlaTyping> ExtractData(IWmdaRepository repo)
        {
            var data = new List<Confidential>();
            var keyword = "Confidential";

            foreach (var line in repo.VersionReport.Where(x => x.StartsWith(keyword)))
            {
                var matched = new Regex($@"^{keyword},(\w+\*)([\w:]+),").Match(line).Groups;
                data.Add(new Confidential(
                    matched[1].Value,
                    matched[2].Value
                    ));
            }

            return data;
        }
    }
}
