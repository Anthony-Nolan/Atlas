namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// Identifies which of the 4 confirmed allowed-loci combinations a
    /// <see cref="DonorImputedGenotype"/> row was computed for. Persisted as an int.
    /// </summary>
    public enum AllowedLociKey
    {
        /// <summary>{A, B, C, DRB1, DQB1}</summary>
        ABCDrb1Dqb1 = 0,

        /// <summary>{A, B, C, DRB1}</summary>
        ABCDrb1 = 1,

        /// <summary>{A, B, DRB1, DQB1}</summary>
        ABDrb1Dqb1 = 2,

        /// <summary>{A, B, DRB1}</summary>
        ABDrb1 = 3,
    }
}
