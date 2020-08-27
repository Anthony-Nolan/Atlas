namespace Atlas.DonorImport.ExternalInterface.Settings
{
    public class StalledFileSettings
    {
        /// <summary>
        ///     This setting is the number of hours after which a donor import file will be considered to be Stalled.
        ///     When stalled files are found, A support notification is sent and the file is marked as such.
        /// </summary>
        public string HoursToCheckStalledFiles { get; set; }
    }
}