namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    public class DonorIdCheckerMessage : BaseDonorCheckMessage
    {
        public DonorIdCheckerMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches) : base($"Donor Id(s) check was finished, {numberOfMismatches} donors not found", requestFileLocation, resultsFilename, numberOfMismatches)
        {
        }
    }
}
