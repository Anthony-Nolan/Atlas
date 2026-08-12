using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Repositories;

[TestFixture]
public class HlaImportRepositoryTests
{
    private const int TypedPositionCount = 12;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void BuildHlaRelations_BuildsRelationForEachPGroupOfEachHla()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);
        var hlaNameId = fixture.Create<int>();
        var pGroupLookup = LookupOf(pGroups);

        var relations = BuildRelations(
            new[] { DonorWithHlaAt(Locus.A, LocusPosition.One, hla) },
            new Dictionary<string, int> { { hlaName, hlaNameId } },
            pGroupLookup,
            NothingProcessed());

        relations.A.Should().BeEquivalentTo(pGroups.Select(pGroup => new HlaNamePGroupRelation
        {
            HlaNameId = hlaNameId,
            PGroupId = pGroupLookup[pGroup]
        }));
    }

    [Test]
    public void BuildHlaRelations_WhenHlaAlreadyProcessed_BuildsNoRelationsForThatHla()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);
        var hlaNameId = fixture.Create<int>();

        var relations = BuildRelations(
            new[] { DonorWithHlaAt(Locus.A, LocusPosition.One, hla) },
            new Dictionary<string, int> { { hlaName, hlaNameId } },
            LookupOf(pGroups),
            AlreadyProcessed(hlaNameId));

        relations.A.Should().BeEmpty();
    }

    [Test]
    public void BuildHlaRelations_WhenMultipleDonorsShareHla_BuildsRelationsOnlyOnce()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hlaNameId = fixture.Create<int>();

        var donors = new[]
        {
            DonorWithHlaAt(Locus.A, LocusPosition.One, new CountingHlaMetadata(hlaName, pGroups)),
            DonorWithHlaAt(Locus.A, LocusPosition.One, new CountingHlaMetadata(hlaName, pGroups))
        };

        var relations = BuildRelations(
            donors,
            new Dictionary<string, int> { { hlaName, hlaNameId } },
            LookupOf(pGroups),
            NothingProcessed());

        relations.A.Should().HaveCount(pGroups.Count);
    }

    /// <summary>
    /// DPB1 relations are deliberately still built, even though <c>ImportProcessedHla</c> discards everything outside
    /// <c>LocusSettings.MatchingOnlyLoci</c> at insert time.
    /// </summary>
    [Test]
    public void BuildHlaRelations_BuildsRelationsAtDpb1()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);

        var relations = BuildRelations(
            new[] { DonorWithHlaAt(Locus.Dpb1, LocusPosition.One, hla) },
            new Dictionary<string, int> { { hlaName, fixture.Create<int>() } },
            LookupOf(pGroups),
            NothingProcessed());

        relations.Dpb1.Should().HaveCount(pGroups.Count);
    }

    [Test]
    public void BuildHlaRelations_WhenLocusIsUntyped_BuildsNoRelationsAtThatLocus()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);

        var relations = BuildRelations(
            new[] { DonorWithHlaAt(Locus.A, LocusPosition.One, hla) },
            new Dictionary<string, int> { { hlaName, fixture.Create<int>() } },
            LookupOf(pGroups),
            NothingProcessed());

        relations.B.Should().BeEmpty();
        relations.C.Should().BeEmpty();
        relations.Dqb1.Should().BeEmpty();
        relations.Drb1.Should().BeEmpty();
    }

    /// <summary>
    /// Every donor's HLA must be read in a single pass. A per-locus pass over the batch would read all twelve positions of
    /// every donor six times over, to consume two positions of one locus each time.
    /// </summary>
    [Test]
    public void BuildHlaRelations_ReadsEachPGroupListOnce()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);

        BuildRelations(
            new[] { DonorWithHlaAtAllPositions(hla) },
            new Dictionary<string, int> { { hlaName, fixture.Create<int>() } },
            LookupOf(pGroups),
            NothingProcessed());

        hla.MatchingPGroupsReads.Should().Be(TypedPositionCount);
    }

    [Test]
    public void BuildHlaRelations_WhenHlaAlreadyProcessed_DoesNotReadPGroups()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(3).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);
        var hlaNameId = fixture.Create<int>();

        BuildRelations(
            new[] { DonorWithHlaAtAllPositions(hla) },
            new Dictionary<string, int> { { hlaName, hlaNameId } },
            LookupOf(pGroups),
            AlreadyProcessed(hlaNameId));

        hla.MatchingPGroupsReads.Should().Be(0);
    }

    /// <summary>
    /// The HLA name, its id, and whether it has already been processed do not depend on the p-group, so they must be
    /// resolved per position, not per candidate p-group.
    /// </summary>
    [Test]
    public void BuildHlaRelations_ReadsHlaNameOncePerPosition()
    {
        var hlaName = fixture.Create<string>();
        var pGroups = fixture.CreateMany<string>(20).ToList();
        var hla = new CountingHlaMetadata(hlaName, pGroups);

        BuildRelations(
            new[] { DonorWithHlaAtAllPositions(hla) },
            new Dictionary<string, int> { { hlaName, fixture.Create<int>() } },
            LookupOf(pGroups),
            NothingProcessed());

        hla.LookupNameReads.Should().Be(TypedPositionCount);
    }

    private static LociInfo<ISet<HlaNamePGroupRelation>> BuildRelations(
        IEnumerable<DonorInfoWithExpandedHla> donors,
        IDictionary<string, int> hlaNameLookup,
        IDictionary<string, int> pGroupLookup,
        LociInfo<ISet<int>> processedHlaIds)
    {
        return HlaImportRepository.BuildHlaRelations(donors.ToList(), hlaNameLookup, pGroupLookup, processedHlaIds);
    }

    private static DonorInfoWithExpandedHla DonorWithHlaAt(Locus locus, LocusPosition position, INullHandledHlaMatchingMetadata hla)
    {
        return new DonorInfoWithExpandedHla
        {
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>().SetPosition(locus, position, hla)
        };
    }

    private static DonorInfoWithExpandedHla DonorWithHlaAtAllPositions(INullHandledHlaMatchingMetadata hla)
    {
        return new DonorInfoWithExpandedHla
        {
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>((_, _) => hla)
        };
    }

    private static LociInfo<ISet<int>> NothingProcessed() => new(_ => new HashSet<int>());

    private static LociInfo<ISet<int>> AlreadyProcessed(int hlaNameId) => new(_ => new HashSet<int> { hlaNameId });

    private static IDictionary<string, int> LookupOf(IEnumerable<string> keys) =>
        keys.Select((key, index) => (key, id: index + 1)).ToDictionary(x => x.key, x => x.id);

    /// <summary>
    /// Counts reads of each property, so that tests can assert on how much work the relation build does, and not just on
    /// what it produces.
    /// </summary>
    private class CountingHlaMetadata : INullHandledHlaMatchingMetadata
    {
        private readonly string lookupName;
        private readonly IList<string> matchingPGroups;

        public int LookupNameReads { get; private set; }
        public int MatchingPGroupsReads { get; private set; }

        public CountingHlaMetadata(string lookupName, IList<string> matchingPGroups)
        {
            this.lookupName = lookupName;
            this.matchingPGroups = matchingPGroups;
        }

        public string LookupName
        {
            get
            {
                LookupNameReads++;
                return lookupName;
            }
        }

        public IList<string> MatchingPGroups
        {
            get
            {
                MatchingPGroupsReads++;
                return matchingPGroups;
            }
        }
    }
}
