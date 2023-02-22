namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    public class DonorIdCheckerNotification
    {
        public string Summary { get; }
        public string Description { get; }

        public DonorIdCheckerNotification(string summary, string description)
        {
            Summary = summary;
            Description = description;
        }
    }
}
