using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class DonorInfoWithTestHlaBuilder
    {
        private readonly DonorInfoWithExpandedHla donor;
        
        public DonorInfoWithTestHlaBuilder(int donorId)
        {
            donor = new DonorInfoWithExpandedHla
            {
                DonorType = DonorType.Adult,
                DonorId = donorId,
                HlaNames = new PhenotypeInfo<string>(),
                MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>()
            };
        }

        public DonorInfoWithTestHlaBuilder WithHla(PhenotypeInfo<IHlaMatchingMetadata> matchingHla)
        {
            donor.HlaNames = new PhenotypeInfo<string>(matchingHla.Map(info => info?.LookupName));
            donor.MatchingHla = matchingHla;
            return this;
        }

        public DonorInfoWithTestHlaBuilder WithHlaAtLocus(Locus locus, TestHlaMetadata hla1, TestHlaMetadata hla2)
        {
            switch (locus)
            {
                case Locus.A:
                    donor.HlaNames.A.Position1 = hla1.OriginalName;
                    donor.HlaNames.A.Position2 = hla2.OriginalName;
                    donor.MatchingHla.A.Position1 = hla1;
                    donor.MatchingHla.A.Position2 = hla2;
                    break;
                case Locus.B:
                    donor.HlaNames.B.Position1 = hla1.OriginalName;
                    donor.HlaNames.B.Position2 = hla2.OriginalName;
                    donor.MatchingHla.B.Position1 = hla1;
                    donor.MatchingHla.B.Position2 = hla2;
                    break;
                case Locus.C:
                    donor.HlaNames.C.Position1 = hla1.OriginalName;
                    donor.HlaNames.C.Position2 = hla2.OriginalName;
                    donor.MatchingHla.C.Position1 = hla1;
                    donor.MatchingHla.C.Position2 = hla2;
                    break;
                case Locus.Dpb1:
                    donor.HlaNames.Dpb1.Position1 = hla1.OriginalName;
                    donor.HlaNames.Dpb1.Position2 = hla2.OriginalName;
                    donor.MatchingHla.Dpb1.Position1 = hla1;
                    donor.MatchingHla.Dpb1.Position2 = hla2;
                    break;
                case Locus.Dqb1:
                    donor.HlaNames.Dqb1.Position1 = hla1.OriginalName;
                    donor.HlaNames.Dqb1.Position2 = hla2.OriginalName;
                    donor.MatchingHla.Dqb1.Position1 = hla1;
                    donor.MatchingHla.Dqb1.Position2 = hla2;
                    break;
                case Locus.Drb1:
                    donor.HlaNames.Drb1.Position1 = hla1.OriginalName;
                    donor.HlaNames.Drb1.Position2 = hla2.OriginalName;
                    donor.MatchingHla.Drb1.Position1 = hla1;
                    donor.MatchingHla.Drb1.Position2 = hla2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
            return this;
        }

        // Populates all null required hla positions (A, B, Drb1) with given hla values
        public DonorInfoWithTestHlaBuilder WithDefaultRequiredHla(TestHlaMetadata hlaMetadata)
        {
            donor.HlaNames.A.Position1 = donor.HlaNames.A.Position1 ?? hlaMetadata.OriginalName;
            donor.HlaNames.A.Position2 = donor.HlaNames.A.Position2 ?? hlaMetadata.OriginalName;
            donor.HlaNames.B.Position1 = donor.HlaNames.B.Position1 ?? hlaMetadata.OriginalName;
            donor.HlaNames.B.Position2 = donor.HlaNames.B.Position2 ?? hlaMetadata.OriginalName;
            donor.HlaNames.Drb1.Position1 = donor.HlaNames.Drb1.Position1 ?? hlaMetadata.OriginalName;
            donor.HlaNames.Drb1.Position2 = donor.HlaNames.Drb1.Position2 ?? hlaMetadata.OriginalName;

            donor.MatchingHla.A.Position1 = donor.MatchingHla.A.Position1 ?? hlaMetadata;
            donor.MatchingHla.A.Position2 = donor.MatchingHla.A.Position2 ?? hlaMetadata;
            donor.MatchingHla.B.Position1 = donor.MatchingHla.B.Position1 ?? hlaMetadata;
            donor.MatchingHla.B.Position2 = donor.MatchingHla.B.Position2 ?? hlaMetadata;
            donor.MatchingHla.Drb1.Position1 = donor.MatchingHla.Drb1.Position1 ?? hlaMetadata;
            donor.MatchingHla.Drb1.Position2 = donor.MatchingHla.Drb1.Position2 ?? hlaMetadata;
            return this;
        }

        public DonorInfoWithTestHlaBuilder WithDonorType(DonorType donorType)
        {
            donor.DonorType = donorType;
            return this;
        }
        
        public DonorInfoWithExpandedHla Build()
        {
            return donor;
        }
    }
}