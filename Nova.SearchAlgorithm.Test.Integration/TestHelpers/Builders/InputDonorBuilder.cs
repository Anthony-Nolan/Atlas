using System;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class InputDonorBuilder
    {
        private readonly InputDonor donor;
        
        public InputDonorBuilder(int donorId)
        {
            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = donorId,
                MatchingHla = new PhenotypeInfo<ExpandedHla>()
            };
        }

        public InputDonorBuilder WithHlaAtLocus(Locus locus, ExpandedHla hla1, ExpandedHla hla2)
        {
            switch (locus)
            {
                case Locus.A:
                    donor.MatchingHla.A_1 = hla1;
                    donor.MatchingHla.A_2 = hla2;
                    break;
                case Locus.B:
                    donor.MatchingHla.B_1 = hla1;
                    donor.MatchingHla.B_2 = hla2;
                    break;
                case Locus.C:
                    donor.MatchingHla.C_1 = hla1;
                    donor.MatchingHla.C_2 = hla2;
                    break;
                case Locus.Dpb1:
                    donor.MatchingHla.DPB1_1 = hla1;
                    donor.MatchingHla.DPB1_2 = hla2;
                    break;
                case Locus.Dqb1:
                    donor.MatchingHla.DQB1_1 = hla1;
                    donor.MatchingHla.DQB1_2 = hla2;
                    break;
                case Locus.Drb1:
                    donor.MatchingHla.DRB1_1 = hla1;
                    donor.MatchingHla.DRB1_2 = hla2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
            return this;
        }

        // Populates all null required hla positions (A, B, DRB) with given hla values
        public InputDonorBuilder WithDefaultRequiredHla(ExpandedHla hla)
        {
            donor.MatchingHla.A_1 = donor.MatchingHla.A_1 ?? hla;
            donor.MatchingHla.A_2 = donor.MatchingHla.A_2 ?? hla;
            donor.MatchingHla.B_1 = donor.MatchingHla.B_1 ?? hla;
            donor.MatchingHla.B_2 = donor.MatchingHla.B_2 ?? hla;
            donor.MatchingHla.DRB1_1 = donor.MatchingHla.DRB1_1 ?? hla;
            donor.MatchingHla.DRB1_2 = donor.MatchingHla.DRB1_2 ?? hla;
            return this;
        }

        public InputDonor Build()
        {
            return donor;
        }
    }
}