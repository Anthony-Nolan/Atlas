namespace Atlas.DonorImport.ExternalInterface.Settings
{
    public class DonorImportSettings
    {
        /// <summary>
        ///     This setting is the number of hours after which a donor import file will be considered to be Stalled.
        ///     When stalled files are found, A support notification is sent and the file is marked as such.
        /// </summary>
        public int HoursToCheckStalledFiles { get; set; }

        /// <summary>
        /// This setting indicates wrether accept or not import files with Full mode.
        /// </summary>
        public bool AllowFullModeImport { get; set; }


    }
}