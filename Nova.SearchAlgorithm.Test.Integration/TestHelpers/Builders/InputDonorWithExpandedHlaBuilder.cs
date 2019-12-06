using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using System;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class InputDonorWithExpandedHlaBuilder
    {
        private readonly InputDonorWithExpandedHla donor;
        
        public InputDonorWithExpandedHlaBuilder(int donorId)
        {
            donor = new InputDonorWithExpandedHla
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = donorId,
                MatchingHla = new PhenotypeInfo<ExpandedHla>()
            };
        }

        public InputDonorWithExpandedHlaBuilder WithMatchingHla(PhenotypeInfo<ExpandedHla> matchingHla)
        {
            donor.MatchingHla = matchingHla;
            return this;
        }

        public InputDonorWithExpandedHlaBuilder WithMatchingHlaAtLocus(Locus locus, ExpandedHla hla1, ExpandedHla hla2)
        {
            switch (locus)
            {
                case Locus.A:
                    donor.MatchingHla.A.Position1 = hla1;
                    donor.MatchingHla.A.Position2 = hla2;
                    break;
                case Locus.B:
                    donor.MatchingHla.B.Position1 = hla1;
                    donor.MatchingHla.B.Position2 = hla2;
                    break;
                case Locus.C:
                    donor.MatchingHla.C.Position1 = hla1;
                    donor.MatchingHla.C.Position2 = hla2;
                    break;
                case Locus.Dpb1:
                    donor.MatchingHla.Dpb1.Position1 = hla1;
                    donor.MatchingHla.Dpb1.Position2 = hla2;
                    break;
                case Locus.Dqb1:
                    donor.MatchingHla.Dqb1.Position1 = hla1;
                    donor.MatchingHla.Dqb1.Position2 = hla2;
                    break;
                case Locus.Drb1:
                    donor.MatchingHla.Drb1.Position1 = hla1;
                    donor.MatchingHla.Drb1.Position2 = hla2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
            return this;
        }

        // Populates all null required hla positions (A, B, Drb1) with given hla values
        public InputDonorWithExpandedHlaBuilder WithDefaultRequiredHla(ExpandedHla hla)
        {
            donor.MatchingHla.A.Position1 = donor.MatchingHla.A.Position1 ?? hla;
            donor.MatchingHla.A.Position2 = donor.MatchingHla.A.Position2 ?? hla;
            donor.MatchingHla.B.Position1 = donor.MatchingHla.B.Position1 ?? hla;
            donor.MatchingHla.B.Position2 = donor.MatchingHla.B.Position2 ?? hla;
            donor.MatchingHla.Drb1.Position1 = donor.MatchingHla.Drb1.Position1 ?? hla;
            donor.MatchingHla.Drb1.Position2 = donor.MatchingHla.Drb1.Position2 ?? hla;
            return this;
        }

        public InputDonorWithExpandedHlaBuilder WithRegistryCode(RegistryCode registryCode)
        {
            donor.RegistryCode = registryCode;
            return this;
        }
        
        public InputDonorWithExpandedHlaBuilder WithDonorType(DonorType donorType)
        {
            donor.DonorType = donorType;
            return this;
        }
        
        public InputDonorWithExpandedHla Build()
        {
            return donor;
        }
    }
}