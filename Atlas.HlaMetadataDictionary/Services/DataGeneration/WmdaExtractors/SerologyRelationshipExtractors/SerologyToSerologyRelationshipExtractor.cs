using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.SerologyRelationshipExtractors
{
    internal class SerologyToSerologyRelationshipExtractor : WmdaDataExtractor<RelSerSer>
    {
        private const string FileName = WmdaFilePathPrefix + "rel_ser_ser.txt";
        private readonly Regex regex = new Regex(@"(\w+)\;(\d*)\;([\d\/]*)\;([\d\/]*)");

        public SerologyToSerologyRelationshipExtractor() : base(FileName)
        {
        }

        protected override RelSerSer MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
            if (!regex.IsMatch(line))
                return null;

            var extractedData = regex.Match(line).Groups;

            return new RelSerSer(
                extractedData[1].Value,
                extractedData[2].Value,
                !extractedData[3].Value.Equals("") ? extractedData[3].Value.Split('/') : new string[] { },
                !extractedData[4].Value.Equals("") ? extractedData[4].Value.Split('/') : new string[] { }
            );
        }
    }
}
