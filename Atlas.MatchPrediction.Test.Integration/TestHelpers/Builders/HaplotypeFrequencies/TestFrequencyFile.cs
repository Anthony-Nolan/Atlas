namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.HaplotypeFrequencies
{
    internal class TestFrequencyFile
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Contents { get; set; }

        public TestFrequencyFile(string fileName)
        {
            FileName = fileName;
            FullPath = fileName;
        }
    }
}
