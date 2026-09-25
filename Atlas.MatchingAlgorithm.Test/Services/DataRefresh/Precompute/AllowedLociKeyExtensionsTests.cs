using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <summary>
/// The four locus sets are a stored contract - a row written under one meaning of <c>ABDrb1</c> is read back under
/// whatever the enum means later - so the sets are pinned here rather than left to be read off the extension class.
/// </summary>
[TestFixture]
public class AllowedLociKeyExtensionsTests
{
    private static readonly object[] ExpectedLoci =
    [
        new object[] { AllowedLociKey.ABCDrb1Dqb1, new[] { Locus.A, Locus.B, Locus.C, Locus.Drb1, Locus.Dqb1 } },
        new object[] { AllowedLociKey.ABCDrb1, new[] { Locus.A, Locus.B, Locus.C, Locus.Drb1 } },
        new object[] { AllowedLociKey.ABDrb1Dqb1, new[] { Locus.A, Locus.B, Locus.Drb1, Locus.Dqb1 } },
        new object[] { AllowedLociKey.ABDrb1, new[] { Locus.A, Locus.B, Locus.Drb1 } }
    ];

    [TestCaseSource(nameof(ExpectedLoci))]
    public void ToLoci_ReturnsExactlyTheNamedLoci(AllowedLociKey key, Locus[] expected)
    {
        key.ToLoci().Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ToLoci_ForEveryKey_ExcludesDpb1()
    {
        // Match prediction never imputes Dpb1, so a combination that allowed it would ask for work the pipeline does
        // not do - and would put a locus in the typing key that can never affect the result.
        foreach (var key in AllowedLociKeyExtensions.All)
        {
            key.ToLoci().Should().NotContain(Locus.Dpb1, $"{key} must not allow Dpb1");
        }
    }

    [Test]
    public void All_HoldsTheFourCombinationsWithNoRepeats()
    {
        AllowedLociKeyExtensions.All.Should().BeEquivalentTo(Enum.GetValues<AllowedLociKey>());
        AllowedLociKeyExtensions.All.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void ToLoci_ForEveryKey_ReturnsADistinctLocusSet()
    {
        // Two keys naming the same set would make the fourth column of the unique index meaningless: the same typing
        // would be computed and stored twice, with identical contents.
        var lociSets = AllowedLociKeyExtensions.All.Select(key => string.Join(",", key.ToLoci().OrderBy(locus => locus)));

        lociSets.Should().OnlyHaveUniqueItems();
    }

    [TestCaseSource(nameof(ExpectedLoci))]
    public void ToAllowedLociKey_ForTheLociAKeyNames_ReturnsThatKey(AllowedLociKey key, Locus[] loci)
    {
        AllowedLociKeyExtensions.ToAllowedLociKey(loci.ToHashSet()).Should().Be(key);
    }

    [Test]
    public void ToLoci_ForAValueOutsideTheEnum_Throws()
    {
        var act = () => ((AllowedLociKey) 99).ToLoci();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ToAllowedLociKey_ForALocusSetNoKeyNames_Throws()
    {
        var act = () => AllowedLociKeyExtensions.ToAllowedLociKey(new HashSet<Locus> { Locus.A });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
