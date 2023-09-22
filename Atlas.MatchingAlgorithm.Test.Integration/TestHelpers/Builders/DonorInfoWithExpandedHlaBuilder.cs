using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using System;

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
                ExternalDonorCode = Guid.NewGuid().ToString(),
                DonorId = donorId,
                HlaNames = new PhenotypeInfo<string>(),
                MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>()
            };
        }

        internal DonorInfoWithTestHlaBuilder WithHla(PhenotypeInfo<INullHandledHlaMatchingMetadata> matchingHla)
        {
            donor.HlaNames = new PhenotypeInfo<string>(matchingHla.Map(info => info?.LookupName));
            donor.MatchingHla = matchingHla;
            return this;
        }

        internal DonorInfoWithTestHlaBuilder WithHlaAtLocus(Locus locus, TestHlaMetadata hla1, TestHlaMetadata hla2)
        {
            donor.HlaNames = donor.HlaNames
                .SetPosition(locus, LocusPosition.One, hla1.LookupName)
                .SetPosition(locus, LocusPosition.Two, hla2.LookupName);

            donor.MatchingHla = donor.MatchingHla
                .SetPosition(locus, LocusPosition.One, hla1)
                .SetPosition(locus, LocusPosition.Two, hla2);
            
            return this;
        }

        // Populates all null required hla positions (A, B, Drb1) with given hla values
        internal DonorInfoWithTestHlaBuilder WithDefaultRequiredHla(TestHlaMetadata hlaMetadata)
        {
            donor.HlaNames = donor.HlaNames
                .SetPosition(Locus.A, LocusPosition.One, donor.HlaNames.A.Position1 ?? hlaMetadata.LookupName)
                .SetPosition(Locus.A, LocusPosition.Two, donor.HlaNames.A.Position2 ?? hlaMetadata.LookupName)
                .SetPosition(Locus.B, LocusPosition.One, donor.HlaNames.B.Position1 ?? hlaMetadata.LookupName)
                .SetPosition(Locus.B, LocusPosition.Two, donor.HlaNames.B.Position2 ?? hlaMetadata.LookupName)
                .SetPosition(Locus.Drb1, LocusPosition.One, donor.HlaNames.Drb1.Position1 ?? hlaMetadata.LookupName)
                .SetPosition(Locus.Drb1, LocusPosition.Two, donor.HlaNames.Drb1.Position2 ?? hlaMetadata.LookupName);
            
            donor.MatchingHla = donor.MatchingHla
                .SetPosition(Locus.A, LocusPosition.One, donor.MatchingHla.A.Position1 ?? hlaMetadata)
                .SetPosition(Locus.A, LocusPosition.Two, donor.MatchingHla.A.Position2 ?? hlaMetadata)
                .SetPosition(Locus.B, LocusPosition.One, donor.MatchingHla.B.Position1 ?? hlaMetadata)
                .SetPosition(Locus.B, LocusPosition.Two, donor.MatchingHla.B.Position2 ?? hlaMetadata)
                .SetPosition(Locus.Drb1, LocusPosition.One, donor.MatchingHla.Drb1.Position1 ?? hlaMetadata)
                .SetPosition(Locus.Drb1, LocusPosition.Two, donor.MatchingHla.Drb1.Position2 ?? hlaMetadata);
            
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