﻿using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Models
{
    [TestFixture]
    internal class ScoringInfoTests
    {
        [Test]
        public void SingleAlleleScoringInfo_WhenSerialisedWithSerologies_ShouldBeSuitablySmall()
        {
            var info = new SingleAlleleScoringInfo(
                "03:212",
                new AlleleTypingStatus(SequenceStatus.Partial, DnaCategory.CDna),
                "03:04P",
                "03:04:01G",
                new[]
                {
                    new SerologyEntry("9", SerologySubtype.Split, false),
                    new SerologyEntry("10", SerologySubtype.Split, false),
                    new SerologyEntry("3", SerologySubtype.Broad, false),
                }
            );

            var infoAsString = JsonConvert.SerializeObject(info);
            var result = new HlaScoringMetadata(Locus.C, "03:212", info, Common.GeneticData.Hla.Models.TypingMethod.Molecular);
            var entity = new HlaMetadataTableRow(result);

            entity.SerialisedHlaInfoType.Should().Be("SingleAlleleScoringInfo");
            entity.SerialisedHlaInfo.Should().Contain(infoAsString);

            infoAsString.Length.Should().BeLessThan(298);        // **This is really critical**. If you are exceeding this, you MUST do an update test and look at the payload size of A*02:01
            infoAsString.Length.Should().Be(208);                // This is just the current length. Adjusting this is fine, but you should be actively thinking about why you're doing it and whether it's necessary / safe.
            infoAsString.Should().Be(                                   // This is just the current string. Adjusting this is fine, but you should be actively thinking about why you're doing it and whether it's necessary.
                @"{""name"":""03:212"",""status"":{""seq"":1,""dna"":1},""pGrp"":""03:04P"",""gGrp"":""03:04:01G"",""ser"":[{""Name"":""9"",""subtype"":2,""direct"":false},{""Name"":""10"",""subtype"":2,""direct"":false},{""Name"":""3"",""subtype"":1,""direct"":false}]}"
            );
        }

        [Test]
        public void SingleAlleleScoringInfo_WhenSerialisedWithOutSerologies_ShouldBeSuitablySmall()
        {
            var info = new SingleAlleleScoringInfo(
                "02:01",
                new AlleleTypingStatus(SequenceStatus.Partial, DnaCategory.CDna),
                "02:01P",
                "02:01:01G",
                null
            );

            var infoAsString = JsonConvert.SerializeObject(info);
            var result = new HlaScoringMetadata(Locus.A, "02:01", info, Common.GeneticData.Hla.Models.TypingMethod.Molecular);
            var entity = new HlaMetadataTableRow(result);

            entity.SerialisedHlaInfoType.Should().Be("SingleAlleleScoringInfo");
            entity.SerialisedHlaInfo.Should().Contain(infoAsString);

            infoAsString.Length.Should().BeLessThan(125);        // **This is really critical**. If you are exceeding this, you MUST do an update test and look at the payload size of A*02:01
            infoAsString.Length.Should().Be(87);                 // This is just the current length. Adjusting this is fine, but you should be actively thinking about why you're doing it and whether it's necessary / safe.
            infoAsString.Should().Be(                                   // This is just the current string. Adjusting this is fine, but you should be actively thinking about why you're doing it and whether it's necessary.
                @"{""name"":""02:01"",""status"":{""seq"":1,""dna"":1},""pGrp"":""02:01P"",""gGrp"":""02:01:01G"",""ser"":[]}"
            );
        }

    }
}
