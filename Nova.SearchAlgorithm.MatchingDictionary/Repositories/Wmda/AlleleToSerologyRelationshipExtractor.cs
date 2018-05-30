using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class AlleleToSerologyRelationshipExtractor : WmdaDataExtractor<RelDnaSer>
    {
        private const string FileName = WmdaFilePathPrefix + "rel_dna_ser";
        private const string RegexPattern = @"^(\w+\*)\;([\w:]+)\;([\d\/\\?]*);([\d\/\\?]*)\;([\d\/\\?]*)\;([\d\/\\?]*)$";

        public AlleleToSerologyRelationshipExtractor() : base(FileName, RegexPattern, TypingMethod.Molecular)
        {
        }

        protected override RelDnaSer MapDataExtractedFromWmdaFile(GroupCollection extractedData)
        {
            var unambiguous = GetAssignments(Assignment.Unambiguous, extractedData[3].Value);
            var possible = GetAssignments(Assignment.Possible, extractedData[4].Value);
            var assumed = GetAssignments(Assignment.Assumed, extractedData[5].Value);
            var expert = GetAssignments(Assignment.Expert, extractedData[6].Value);
            var assignments = unambiguous.Union(possible.Union(assumed.Union(expert)));

            return new RelDnaSer(
                extractedData[1].Value,
                extractedData[2].Value,
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
