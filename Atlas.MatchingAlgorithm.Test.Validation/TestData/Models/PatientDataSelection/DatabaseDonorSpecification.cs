using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    /// <summary>
    /// A set of criteria used to generate/select a database donor
    /// (i.e. a 'Donor' in algorithm terminology. 'Database Donor' used to distinguish from 'Meta-Donor's)
    /// </summary>
    public class DatabaseDonorSpecification
    {
        /// <summary>
        /// Determines to what resolution the expected matched donor is typed
        /// </summary>
        public PhenotypeInfo<HlaTypingResolution> MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        
        /// <summary>
        /// Determines whether the hla at each position should match the meta-donor's genotype.
        /// If false, the hla will be instead selected from a 'non-matching' hla allele source
        /// </summary>
        public PhenotypeInfo<bool> ShouldMatchGenotype { get; set; } = new PhenotypeInfo<bool>(true);

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatabaseDonorSpecification) obj);
        }

        private bool Equals(DatabaseDonorSpecification other)
        {
            return Equals(MatchingTypingResolutions, other.MatchingTypingResolutions) && Equals(ShouldMatchGenotype, other.ShouldMatchGenotype);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((MatchingTypingResolutions != null ? MatchingTypingResolutions.GetHashCode() : 0) * 397) ^ (ShouldMatchGenotype != null ? ShouldMatchGenotype.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DatabaseDonorSpecification left, DatabaseDonorSpecification right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DatabaseDonorSpecification left, DatabaseDonorSpecification right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}