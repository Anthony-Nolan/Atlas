using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class AlleleLevelMatchCriteriaFromExpandedHla
    {
        private readonly Locus locusUnderTest;
        private readonly DonorType donorType;
        private readonly PhenotypeInfo<ExpandedHla> phenotype;

        public AlleleLevelMatchCriteriaFromExpandedHla(
            Locus locusUnderTest,
            DonorType donorType,
            PhenotypeInfo<ExpandedHla> phenotype)
        {
            this.locusUnderTest = locusUnderTest;
            this.donorType = donorType;
            this.phenotype = phenotype;
        }

        /// <summary>
        /// Uses AlleleLevelMatchCriteriaBuilder to build criteria with 
        /// the specified number of mismatches at the locus under test.
        /// </summary>
        public AlleleLevelMatchCriteria GetAlleleLevelMatchCriteria(int mismatchCount = 0)
        {
            var builder = GetDefaultBuilder();

            if (mismatchCount > 0)
            {
                builder
                    .WithDonorMismatchCount(mismatchCount)
                    .WithLocusMatchCriteria(locusUnderTest, GetLocusMatchCriteria(locusUnderTest, mismatchCount));
            }
                              
            return builder.Build();
        }

        private AlleleLevelMatchCriteriaBuilder GetDefaultBuilder()
        {
            const int zeroMismatchCount = 0;
            return new AlleleLevelMatchCriteriaBuilder()
                .WithSearchType(donorType)
                .WithLocusMatchCriteria(Locus.A, GetLocusMatchCriteria(Locus.A, zeroMismatchCount))
                .WithLocusMatchCriteria(Locus.B, GetLocusMatchCriteria(Locus.B, zeroMismatchCount))
                .WithLocusMatchCriteria(Locus.Drb1, GetLocusMatchCriteria(Locus.Drb1, zeroMismatchCount))
                .WithDonorMismatchCount(zeroMismatchCount);
        }

        private AlleleLevelLocusMatchCriteria GetLocusMatchCriteria(
            Locus locus,
            int mismatchCount)
        {
            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatchCount,
                PGroupsToMatchInPositionOne = GetPGroupsAt(locus, TypePositions.One),
                PGroupsToMatchInPositionTwo = GetPGroupsAt(locus, TypePositions.Two)
            };
        }

        private IEnumerable<string> GetPGroupsAt(Locus locus, TypePositions typePosition)
        {
            return phenotype.DataAtPosition(locus, typePosition).PGroups;
        }
    }
}
