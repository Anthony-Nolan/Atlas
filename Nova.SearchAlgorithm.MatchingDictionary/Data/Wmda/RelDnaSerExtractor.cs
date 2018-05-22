using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    internal class RelDnaSerExtractor : IWmdaDataExtractor
    {
        public IEnumerable<IWmdaHlaType> ExtractData(IWmdaRepository repo)
        {
            var data = new List<RelDnaSer>();

            foreach (var line in repo.RelDnaSer)
            {
                var matched = new Regex(@"^(\w+\*)\;([\w:]+)\;([\d\/\\?]*);([\d\/\\?]*)\;([\d\/\\?]*)\;([\d\/\\?]*)$").Match(line).Groups;

                var unambiguous = GetAssignments(Assignment.Unambiguous, matched[3].Value);
                var possible = GetAssignments(Assignment.Possible, matched[4].Value);
                var assumed = GetAssignments(Assignment.Assumed, matched[5].Value);
                var expert = GetAssignments(Assignment.Expert, matched[6].Value);
                var assignments = unambiguous.Union(possible.Union(assumed.Union(expert)));

                data.Add(new RelDnaSer(
                    matched[1].Value,
                    matched[2].Value,
                    assignments
                ));
            }

            return data;
        }

        private static IEnumerable<SerologyAssignment> GetAssignments(Assignment assignment, string rawString)
        {
            var serologies = rawString.Split('/').ToList();
            serologies.RemoveAll(s => s.Equals("0") || s.Equals("?") || s.Equals(""));
            serologies.Sort();

            return serologies.Any() ? 
                serologies.Select(s => new SerologyAssignment(s, assignment)) : 
                new List<SerologyAssignment>();
        }
    }
}
