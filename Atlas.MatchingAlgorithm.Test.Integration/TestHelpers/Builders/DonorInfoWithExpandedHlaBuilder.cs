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

        internal DonorInfoWithTestHlaBuilder(int donorId)
        {
            donor = new DonorInfoWithExpandedHla
            {
                DonorType = DonorType.Adult,
                DonorId = donorId,
                HlaNames = new PhenotypeInfo<string>(),
                MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>()
            };
        }

        internal DonorInfoWithTestHlaBuilder WithHla(PhenotypeInfo<IHlaMatchingMetadata> matchingHla)
        {
            donor.HlaNames = new PhenotypeInfo<string>(matchingHla.Map(info => info?.LookupName));
            donor.MatchingHla = matchingHla;
            return this;
        }

        internal DonorInfoWithTestHlaBuilder WithHlaAtLocus(Locus locus, TestHlaMetadata hla1, TestHlaMetadata hla2)
        {
            switch (locus)
            {
                case Locus.A:
                    donor.HlaNames.A = donor.HlaNames.A.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.A = donor.HlaNames.A.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.A = donor.MatchingHla.A.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.A = donor.MatchingHla.A.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                case Locus.B:
                    donor.HlaNames.B = donor.HlaNames.B.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.B = donor.HlaNames.B.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.B = donor.MatchingHla.B.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.B = donor.MatchingHla.B.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                case Locus.C:
                    donor.HlaNames.C = donor.HlaNames.C.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.C = donor.HlaNames.C.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.C = donor.MatchingHla.C.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.C = donor.MatchingHla.C.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                case Locus.Dpb1:
                    donor.HlaNames.Dpb1 = donor.HlaNames.Dpb1.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.Dpb1 = donor.HlaNames.Dpb1.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.Dpb1 = donor.MatchingHla.Dpb1.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.Dpb1 = donor.MatchingHla.Dpb1.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                case Locus.Dqb1:
                    donor.HlaNames.Dqb1 = donor.HlaNames.Dqb1.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.Dqb1 = donor.HlaNames.Dqb1.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.Dqb1 = donor.MatchingHla.Dqb1.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.Dqb1 = donor.MatchingHla.Dqb1.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                case Locus.Drb1:
                    donor.HlaNames.Drb1 = donor.HlaNames.Drb1.SetAtPosition(LocusPosition.One, hla1.OriginalName);
                    donor.HlaNames.Drb1 = donor.HlaNames.Drb1.SetAtPosition(LocusPosition.Two, hla2.OriginalName);
                    donor.MatchingHla.Drb1 = donor.MatchingHla.Drb1.SetAtPosition(LocusPosition.One, hla1);
                    donor.MatchingHla.Drb1 = donor.MatchingHla.Drb1.SetAtPosition(LocusPosition.Two, hla2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }

        // Populates all null required hla positions (A, B, Drb1) with given hla values
        internal DonorInfoWithTestHlaBuilder WithDefaultRequiredHla(TestHlaMetadata hlaMetadata)
        {
            donor.HlaNames.A = donor.HlaNames.A.SetAtPosition(LocusPosition.One, donor.HlaNames.A.Position1 ?? hlaMetadata.OriginalName);
            donor.HlaNames.A = donor.HlaNames.A.SetAtPosition(LocusPosition.Two, donor.HlaNames.A.Position2 ?? hlaMetadata.OriginalName);
            donor.HlaNames.B = donor.HlaNames.B.SetAtPosition(LocusPosition.One, donor.HlaNames.B.Position1 ?? hlaMetadata.OriginalName);
            donor.HlaNames.B = donor.HlaNames.B.SetAtPosition(LocusPosition.Two, donor.HlaNames.B.Position2 ?? hlaMetadata.OriginalName);
            donor.HlaNames.Drb1 = donor.HlaNames.Drb1.SetAtPosition(LocusPosition.One, donor.HlaNames.Drb1.Position1 ?? hlaMetadata.OriginalName);
            donor.HlaNames.Drb1 = donor.HlaNames.Drb1.SetAtPosition(LocusPosition.Two, donor.HlaNames.Drb1.Position2 ?? hlaMetadata.OriginalName);

            donor.MatchingHla.A = donor.MatchingHla.A.SetAtPosition(LocusPosition.One, donor.MatchingHla.A.Position1 ?? hlaMetadata);
            donor.MatchingHla.A = donor.MatchingHla.A.SetAtPosition(LocusPosition.Two, donor.MatchingHla.A.Position2 ?? hlaMetadata);
            donor.MatchingHla.B = donor.MatchingHla.B.SetAtPosition(LocusPosition.One, donor.MatchingHla.B.Position1 ?? hlaMetadata);
            donor.MatchingHla.B = donor.MatchingHla.B.SetAtPosition(LocusPosition.Two, donor.MatchingHla.B.Position2 ?? hlaMetadata);
            donor.MatchingHla.Drb1 = donor.MatchingHla.Drb1.SetAtPosition(LocusPosition.One, donor.MatchingHla.Drb1.Position1 ?? hlaMetadata);
            donor.MatchingHla.Drb1 = donor.MatchingHla.Drb1.SetAtPosition(LocusPosition.Two, donor.MatchingHla.Drb1.Position2 ?? hlaMetadata);
            return this;
        }

        internal DonorInfoWithTestHlaBuilder WithDonorType(DonorType donorType)
        {
            donor.DonorType = donorType;
            return this;
        }

        internal DonorInfoWithExpandedHla Build() => donor;
    }
}