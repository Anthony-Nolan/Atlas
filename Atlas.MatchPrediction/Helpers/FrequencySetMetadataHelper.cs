using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Helpers
{
    internal static class FrequencySetMetadataHelper
    {
        public static HaplotypeFrequencySet GetFrequencySetMetadataFromFileName(string fileName)
        {
            var filePathSections = fileName.Split('/');

            return new HaplotypeFrequencySet()
            {
                Registry = filePathSections.Length > 0 ? filePathSections[0] : null,
                Ethnicity = filePathSections.Length == 3 ? filePathSections[1] : null,
                Active = true
            };
        }
    }
}
