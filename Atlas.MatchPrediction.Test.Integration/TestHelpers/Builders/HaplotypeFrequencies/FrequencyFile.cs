namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.HaplotypeFrequencies
{
    internal class FrequencyFile
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Contents { get; set; }

        public FrequencyFile(string fileName)
        {
            FileName = fileName;
            FullPath = fileName;
        }
    }
}
