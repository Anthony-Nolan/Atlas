using System;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Client.Models.Donors
{
    /// It could be argued that these donor update models belong to the Donor Import component,
    /// as it is responsible for detecting and publishing this information.
    /// However, as matching algorithm is currently the only consumer, and the model only includes
    /// properties need by matching, it seems ok to leave these here, and avoid a breaking client change.
    /// But it would make sense to migrate these models in the unlikely event that an additional component subscribes to the same updates.

    /// <summary>
    /// Donor info containing only the information required to search for donors - e.g. by the search algorithm service
    /// This only includes the bare minimum for a search for performance reasons.
    /// </summary>
    public class SearchableDonorInformation
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DPB1_1 { get; set; }
        public string DPB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
    }

    public class SearchableDonorUpdate
    {
        public DateTimeOffset PublishedDateTime { get; set; } = DateTimeOffset.UtcNow;
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public SearchableDonorInformation SearchableDonorInformation { get; set; }
    }
}