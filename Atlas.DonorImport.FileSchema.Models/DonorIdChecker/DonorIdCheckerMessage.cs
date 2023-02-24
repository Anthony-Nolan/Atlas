namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    public class DonorIdCheckerMessage
    {
        public string Summary { get; }
        public string ResultsFilename { get; }

        public DonorIdCheckerMessage(string summary, string resultsFilename)
        {
            Summary = summary;
            ResultsFilename = resultsFilename;
        }
    }
}
