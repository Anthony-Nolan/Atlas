﻿using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration
{
    [TestFixture]
    public class SmallGGroupsServiceTests
    {
        private ISmallGGroupsBuilder builder;
        private ISmallGGroupsService service;

        [SetUp]
        public void SetUp()
        {
            builder = Substitute.For<ISmallGGroupsBuilder>();
            service = new SmallGGroupsService(builder);

            builder.BuildSmallGGroups(default).ReturnsForAnyArgs(new List<SmallGGroup>());
        }

        [Test]
        public void GetSmallGGroupMetadata_BuildsSmallGGroupsToRequestedHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "version";

            service.GetSmallGGroupMetadata(hlaNomenclatureVersion);

            builder.Received(1).BuildSmallGGroups(hlaNomenclatureVersion);
        }

        [TestCase("01:01", new object[] { "01:01", "01:XX" })]
        [TestCase("01:01N", new object[] { "01:01N", "01:01", "01:XX" })]
        [TestCase("01:01:01", new object[] { "01:01:01", "01:01", "01:XX" })]
        [TestCase("01:01:01Q", new object[] { "01:01:01Q", "01:01", "01:01Q", "01:XX" })]
        [TestCase("01:01:01:01", new object[] { "01:01:01:01", "01:01", "01:XX" })]
        [TestCase("01:01:01:01L", new object[] { "01:01:01:01L", "01:01", "01:01L", "01:XX" })]
        public void GetSmallGGroupMetadata_ReturnsMetadataForEachPossibleLookupName(string alleleName, object[] expectedLookupNames)
        {
            var smallGGroup = SmallGGroupBuilder.Default
                .WithAllele(alleleName)
                .Build(1);

            builder.BuildSmallGGroups(default).ReturnsForAnyArgs(smallGGroup);

            var result = service.GetSmallGGroupMetadata(default).ToList();

            result.Select(r => r.LookupName).Should().BeEquivalentTo(expectedLookupNames);
            result.Count.Should().Be(expectedLookupNames.Length);
        }

        [Test]
        public void GetSmallGGroupMetadata_GroupsMetadataByLocusAndLookupName()
        {
            const Locus locus = Locus.B;
            const string firstField = "01";

            const string gGroup1 = "g-group-1";
            const string allele1 = firstField + ":01";
            var smallGGroup1 = SmallGGroupBuilder.Default
                .With(x => x.Locus, locus)
                .With(x => x.Name, gGroup1)
                .WithAllele(allele1)
                .Build();

            const string gGroup2 = "g-group-2";
            const string allele2 = firstField + ":02";
            var smallGGroup2 = SmallGGroupBuilder.Default
                .With(x => x.Locus, locus)
                .With(x => x.Name, gGroup2)
                .WithAllele(allele2)
                .Build();

            builder.BuildSmallGGroups(default).ReturnsForAnyArgs(new[] { smallGGroup1, smallGGroup2 });

            var result = service.GetSmallGGroupMetadata(default).ToList();

            // both alleles should result in the same XX code lookup name
            var xxCodeMetadata = result.Single(r => r.LookupName == firstField + ":XX");
            xxCodeMetadata.Locus.Should().Be(locus);
            xxCodeMetadata.SmallGGroups.Should().BeEquivalentTo(gGroup1, gGroup2);
        }
    }
}