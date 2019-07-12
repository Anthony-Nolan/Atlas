using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings
{
    internal class UpdateSettingsBody
    {
        public string kind { get; set; }
        public Dictionary<string, string> properties { get; set; }
    }
}