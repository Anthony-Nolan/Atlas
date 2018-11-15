using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.SerologyRelationshipExtractors
{
    internal class AlleleToSerologyRelationshipExtractor : WmdaDataExtractor<RelDnaSer>
    {
        private const string FileName = WmdaFilePathPrefix + "rel_dna_ser.txt";
        private readonly Regex regex = new Regex(@"^(\w+\*)\;([\w:]+)\;([\d\/\\?]*);([\d\/\\?]*)\;([\d\/\\?]*)\;([\d\/\\?]*)$");

        public AlleleToSerologyRelationshipExtractor() : base(FileName)
        {
        }

        protected override RelDnaSer MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            if (!regex.IsMatch(line))
                return null;

            var extractedData = regex.Match(line).Groups;

            var unambiguous = GetAssignments(Assignment.Unambiguous, extractedData[3].Value);
            var possible = GetAssignments(Assignment.Possible, extractedData[4].Value);
            var assumed = GetAssignments(Assignment.Assumed, extractedData[5].Value);
            var expert = GetAssignments(Assignment.Expert, extractedData[6].Value);

            var allAssignments = unambiguous.Union(possible.Union(assumed.Union(expert)));

            return new RelDnaSer(
                extractedData[1].Value,
                extractedData[2].Value,
                allAssignments
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
