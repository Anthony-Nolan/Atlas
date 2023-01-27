using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Client.Models.Donors
{
    public enum DonorType
    {
        // Do not renumber, these values are stored in the database as integers.
        [StringValue("adult"), StringValue("a")]
        Adult = 1, // AKA: MUD

        [StringValue("cord"), StringValue("c")]
        Cord = 2 // AKA: CBU
    }
}