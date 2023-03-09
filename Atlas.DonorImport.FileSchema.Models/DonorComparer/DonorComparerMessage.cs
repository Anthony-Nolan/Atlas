namespace Atlas.DonorImport.FileSchema.Models.DonorComparer
{
    public class DonorComparerMessage : BaseDonorCheckMessage
    {
        public DonorComparerMessage(string requestFileLocation, string resultsFilename, int numberOfMismatches) : base($"Donor(s) comparison was finished, {numberOfMismatches} donor differences found", requestFileLocation, resultsFilename, numberOfMismatches)
        {
        }
        
    }
}
