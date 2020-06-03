using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors
{
    internal class Dpb1TceGroupAssignmentExtractor : WmdaDataExtractor<Dpb1TceGroupAssignment>
    {
        private const string FileName = "tce/dpb_tce.csv";
        private readonly Regex regex = new Regex(@"^DPB1\*([\w:]+),.*,(\w?).*,(\w?).*,");

        public Dpb1TceGroupAssignmentExtractor() : base(FileName)
        {
        }

        protected override Dpb1TceGroupAssignment MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
            if (!regex.IsMatch(line))
            {
                return null;
            }

            var extractedData = regex.Match(line).Groups;

            var alleleName = extractedData[1].Value;
            var vOneAssignment = GetAssignment(extractedData[2].Value);
            var vTwoAssignment = GetAssignment(extractedData[3].Value);

            return new Dpb1TceGroupAssignment(
                alleleName,
                vOneAssignment,
                vTwoAssignment
            );
        }

        protected string GetAssignment(string assignmentString)
        {
            return assignmentString.Equals("0")
                ? string.Empty
                : assignmentString;
        }
    }
}