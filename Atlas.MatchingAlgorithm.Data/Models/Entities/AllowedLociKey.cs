namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// Identifies which of the 4 confirmed allowed-loci combinations a <see cref="SubjectGenotypeSetValue"/> /
    /// <see cref="DonorSubjectGenotypeSet"/> row is for. Persisted as its member name (string) so the stored data
    /// is self-describing.
    /// </summary>
    public enum AllowedLociKey
    {
        /// <summary>{A, B, C, DRB1, DQB1}</summary>
        ABCDrb1Dqb1 = 1,

        /// <summary>{A, B, C, DRB1}</summary>
        ABCDrb1 = 2,

        /// <summary>{A, B, DRB1, DQB1}</summary>
        ABDrb1Dqb1 = 3,

        /// <summary>{A, B, DRB1}</summary>
        ABDrb1 = 4,
    }
}
