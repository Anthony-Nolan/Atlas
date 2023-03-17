namespace Atlas.DonorImport.FileSchema.Models.DonorChecker
{
    public class DonorCheckerMessage
    {
        public DonorCheckerMessage(string requestFileLocation, int resultsCount, string resultsFilename)
        {
            RequestFileLocation = requestFileLocation;
            ResultsCount = resultsCount;
            ResultsFilename = resultsCount > 0 ? resultsFilename : string.Empty;
        }

        public string RequestFileLocation { get; }
        public int ResultsCount { get; }
        public string ResultsFilename { get; }
    }
}
