namespace Atlas.MatchingAlgorithm.Client.Models
{
    /// <summary>
    /// Registries accepted by the serach algorithm.
    /// Other registries exist, but searches will be rejected if created with any registries not in this enum
    /// </summary>
    public enum RegistryCode
    {
        // Do not renumber, these values are stored in the (non-SOLAR) database as integers.
        AN = 1, // Anthony Nolan
        NHSBT = 2, // NHS Blood Transfusion. AKA: BBMR
        WBS = 3, // Welsh Blood Service. AKA: WBMDR
        DKMS = 4, // German Marrow Donor Program
        FRANCE = 5,
        NMDP = 6, // AKA: US
        ITALY = 7
    }
}
