namespace Atlas.DonorImport.FileSchema.Models
{
    public abstract class BaseDonorCheckMessage
    {
        protected BaseDonorCheckMessage(string summary, string requestFileLocation, string resultsFilename, int numberOfMismatches)
        {
            Summary = summary;
            RequestFileLocation = requestFileLocation;
            ResultsFilename = numberOfMismatches > 0 ? resultsFilename : string.Empty;
            NumberOfMismatches = numberOfMismatches;
        }

        public string Summary { get; }
        public string RequestFileLocation { get; }
        public int NumberOfMismatches { get; }
        public string ResultsFilename { get; }
    }
}
