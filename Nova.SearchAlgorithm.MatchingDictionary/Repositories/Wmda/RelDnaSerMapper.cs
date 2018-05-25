using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class RelDnaSerMapper : IWmdaDataMapper<RelDnaSer>
    {
        public RelDnaSer MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            var unambiguous = GetAssignments(Assignment.Unambiguous, matched[3].Value);
            var possible = GetAssignments(Assignment.Possible, matched[4].Value);
            var assumed = GetAssignments(Assignment.Assumed, matched[5].Value);
            var expert = GetAssignments(Assignment.Expert, matched[6].Value);
            var assignments = unambiguous.Union(possible.Union(assumed.Union(expert)));

            return new RelDnaSer(
                matched[1].Value,
                matched[2].Value,
                assignments
            );
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
