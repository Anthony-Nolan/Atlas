using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <summary>
/// The typing key decides which donors share a stored payload, so every property tested here is a correctness
/// property: a key that collides serves one donor another donor's genotypes, and a key that varies where it should not
/// recomputes the most expensive operation in the system for nothing.
/// </summary>
[TestFixture]
public class SubjectGenotypeSetKeyGeneratorTests
{
    /// <inheritdoc cref="SubjectGenotypeSetKeyGenerator" />
    /// <remarks>
    /// Declared here rather than read from the generator, so that the canonical strings below are pinned against a
    /// second, independent copy of the separator. Reading the generator's own constant would make them agree with it
    /// by construction and assert nothing.
    /// </remarks>
    private const char Separator = '\u001F';

    [Test]
    public void GenerateHlaTypingKey_ReturnsALowerCaseHexSha256Digest()
    {
        // Hex, not Base64. The column is nvarchar(64) under a case-insensitive collation, so two digests differing
        // only in letter case would be one row to SQL Server and two distinct strings to C#.
        var key = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(TypedAtEveryLocus(), AllowedLociKey.ABCDrb1Dqb1);

        key.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Test]
    public void GenerateHlaTypingKey_ForTheSameTypingAndCombination_ReturnsTheSameKey()
    {
        var first = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(TypedAtEveryLocus(), AllowedLociKey.ABCDrb1Dqb1);
        var second = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(TypedAtEveryLocus(), AllowedLociKey.ABCDrb1Dqb1);

        second.Should().Be(first);
    }

    [Test]
    public void GenerateHlaTypingKey_ForTypingsDifferingOnlyOutsideTheCombination_ReturnsTheSameKey()
    {
        // The whole point of keying per combination: two donors identical at {A, B, DRB1} share one ABDrb1 payload,
        // however far apart they are at C and DQB1. Every stage of imputation gates on the allowed loci, so nothing
        // outside them can reach the result.
        var typing = TypedAtEveryLocus();
        var differentElsewhere = new PhenotypeInfoBuilder<string>(typing)
            .WithDataAt(Locus.C, "c-other-1", "c-other-2")
            .WithDataAt(Locus.Dqb1, "dqb1-other-1", "dqb1-other-2")
            .WithDataAt(Locus.Dpb1, "dpb1-other-1", "dpb1-other-2")
            .Build();

        var key = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, AllowedLociKey.ABDrb1);
        var otherKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(differentElsewhere, AllowedLociKey.ABDrb1);

        otherKey.Should().Be(key);
    }

    [TestCase(Locus.A)]
    [TestCase(Locus.B)]
    [TestCase(Locus.C)]
    [TestCase(Locus.Drb1)]
    [TestCase(Locus.Dqb1)]
    public void GenerateHlaTypingKey_ForTypingsDifferingAtALocusInTheCombination_ReturnsDifferentKeys(Locus locus)
    {
        var typing = TypedAtEveryLocus();
        var different = new PhenotypeInfoBuilder<string>(typing).WithDataAt(locus, "other-1", "other-2").Build();

        var key = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, AllowedLociKey.ABCDrb1Dqb1);
        var otherKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(different, AllowedLociKey.ABCDrb1Dqb1);

        otherKey.Should().NotBe(key);
    }

    [Test]
    public void GenerateHlaTypingKey_ForTypingsDifferingOnlyAtPositionOrder_ReturnsDifferentKeys()
    {
        // Positions are not interchangeable to imputation, so they must not be to the key either.
        var typing = TypedAtEveryLocus();
        var swapped = new PhenotypeInfoBuilder<string>(typing).WithDataAt(Locus.A, "a-2", "a-1").Build();

        var key = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, AllowedLociKey.ABCDrb1Dqb1);
        var swappedKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(swapped, AllowedLociKey.ABCDrb1Dqb1);

        swappedKey.Should().NotBe(key);
    }

    [Test]
    public void GenerateHlaTypingKey_ForTheSameTypingAtDifferentCombinations_ReturnsDifferentKeys()
    {
        var typing = TypedAtEveryLocus();

        var keys = AllowedLociKeyExtensions.All
            .Select(allowedLociKey => SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, allowedLociKey))
            .ToList();

        keys.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void GenerateHlaTypingKey_ForNullAndEmptyAtTheSamePosition_ReturnsTheSameKey()
    {
        // The matching algorithm reads both as an untyped position, and the precompute service imputes an empty name as
        // null, so two donors that differ only this way must share one row.
        var withNull = new PhenotypeInfoBuilder<string>(TypedAtEveryLocus()).WithDataAt(Locus.B, null, null).Build();
        var withEmpty = new PhenotypeInfoBuilder<string>(TypedAtEveryLocus()).WithDataAt(Locus.B, "", "").Build();

        var nullKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(withNull, AllowedLociKey.ABDrb1);
        var emptyKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(withEmpty, AllowedLociKey.ABDrb1);

        emptyKey.Should().Be(nullKey);
    }

    [Test]
    public void Canonicalise_JoinsOnlyTheAllowedLociInTheFrozenOrder()
    {
        // Pinned, not derived. This string is what every stored key was hashed from, so a change to it silently
        // invalidates every row already written: nothing would be found, and everything would be recomputed.
        var canonical = SubjectGenotypeSetKeyGenerator.Canonicalise(TypedAtEveryLocus(), AllowedLociKey.ABCDrb1Dqb1.ToLoci());

        canonical.Should().Be(
            $"a-1{Separator}a-2{Separator}b-1{Separator}b-2{Separator}c-1{Separator}c-2{Separator}" +
            $"drb1-1{Separator}drb1-2{Separator}dqb1-1{Separator}dqb1-2{Separator}");
    }

    [Test]
    public void Canonicalise_ForACombinationWithoutCOrDqb1_OmitsThemEntirely()
    {
        // Omitted rather than written as empty: a placeholder would make ABDrb1 and an untyped ABCDrb1Dqb1 hash the
        // same string, and the two ask for different imputations.
        var canonical = SubjectGenotypeSetKeyGenerator.Canonicalise(TypedAtEveryLocus(), AllowedLociKey.ABDrb1.ToLoci());

        canonical.Should().Be($"a-1{Separator}a-2{Separator}b-1{Separator}b-2{Separator}drb1-1{Separator}drb1-2{Separator}");
    }

    [Test]
    public void Canonicalise_NeverIncludesDpb1()
    {
        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            var canonical = SubjectGenotypeSetKeyGenerator.Canonicalise(TypedAtEveryLocus(), allowedLociKey.ToLoci());

            canonical.Should().NotContain("dpb1", $"{allowedLociKey} must not carry Dpb1 into the key");
        }
    }

    private static PhenotypeInfo<string> TypedAtEveryLocus() =>
        new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, "a-1", "a-2")
            .WithDataAt(Locus.B, "b-1", "b-2")
            .WithDataAt(Locus.C, "c-1", "c-2")
            .WithDataAt(Locus.Dpb1, "dpb1-1", "dpb1-2")
            .WithDataAt(Locus.Dqb1, "dqb1-1", "dqb1-2")
            .WithDataAt(Locus.Drb1, "drb1-1", "drb1-2")
            .Build();
}
