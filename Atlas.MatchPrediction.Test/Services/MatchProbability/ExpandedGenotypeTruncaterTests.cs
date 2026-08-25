using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.MatchProbability;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

/// <summary>
/// Characterisation of <see cref="ExpandedGenotypeTruncater"/>. What the truncater does on a TIE decides which
/// genotypes a capped donor keeps, so it is a clinical output, and any change to how the top N is selected has to
/// keep passing these.
///
/// <para>
/// <b>The rule these tests pin: on equal likelihood, the first-inserted key wins.</b> The selection is ordered by
/// <c>(likelihood descending, insertion order ascending)</c>, and insertion order is pairing order, which is survivor
/// order, which is the projected pool's order - the chain <c>FrequencySetCacheEntry.ProjectPool</c> is deliberately
/// written to preserve.
/// </para>
///
/// <para>
/// Two consequences worth stating, because a bounded heap gets them wrong unless it is told not to:
/// <see cref="TruncateGenotypes_WhenLikelihoodsTie_KeepsTheEarliestInserted"/> (which of the tied keys survive) and
/// <see cref="TruncateGenotypes_SumsTheKeptLikelihoodsInDescendingLikelihoodOrder"/> (the ORDER the kept likelihoods
/// are summed in, which <c>decimal</c> addition can see).
/// </para>
/// </summary>
[TestFixture]
internal class ExpandedGenotypeTruncaterTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void TruncateGenotypes_WhenGenotypesAreWithinTheCap_KeepsEveryGenotypeAndLikelihood()
    {
        var first = DistinctGenotype();
        var second = DistinctGenotype();

        var result = Truncate(cap: 10, (first, 0.4m), (second, 0.1m));

        result.Genotypes.Should().BeEquivalentTo(new[] { first, second });
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [first.ToHlaNames()] = 0.4m,
            [second.ToHlaNames()] = 0.1m
        });
        result.SumOfLikelihoods.Should().Be(0.5m);
    }

    [Test]
    public void TruncateGenotypes_WhenGenotypesExceedTheCap_KeepsTheMostLikelyAndDropsTheRest()
    {
        var cheapest = DistinctGenotype();
        var dearest = DistinctGenotype();
        var middle = DistinctGenotype();

        // Deliberately inserted cheapest-first, so a selection that ignored the value and took the first N would fail.
        var result = Truncate(cap: 2, (cheapest, 0.01m), (dearest, 0.4m), (middle, 0.2m));

        result.GenotypeLikelihoods.Keys.Should().BeEquivalentTo(new[] { dearest.ToHlaNames(), middle.ToHlaNames() });
        result.Genotypes.Should().BeEquivalentTo(new[] { dearest, middle });

        // The sum is over the KEPT genotypes only - truncation deliberately changes it, and MatchProbabilityService
        // divides by it, so this is the number the whole prediction is normalised against.
        result.SumOfLikelihoods.Should().Be(0.6m);
    }

    [Test]
    public void TruncateGenotypes_WhenLikelihoodsTie_KeepsTheEarliestInserted()
    {
        var first = DistinctGenotype();
        var second = DistinctGenotype();
        var third = DistinctGenotype();

        // Three-way tie, cap 2. A stable sort keeps the two earliest; an unstable one, or a bounded heap that evicts on
        // '>=' rather than '>', can keep any two. Which pair survives is a clinical output, not an implementation
        // detail: these genotypes go on to be scored against the patient.
        var result = Truncate(cap: 2, (first, 0.25m), (second, 0.25m), (third, 0.25m));

        result.GenotypeLikelihoods.Keys.Should().BeEquivalentTo(new[] { first.ToHlaNames(), second.ToHlaNames() });
        result.Genotypes.Should().BeEquivalentTo(new[] { first, second });
    }

    [Test]
    public void TruncateGenotypes_WhenATieStraddlesTheCap_KeepsTheDearerKeysThenTheEarlierOfTheTied()
    {
        var dearest = DistinctGenotype();
        var tiedFirst = DistinctGenotype();
        var tiedSecond = DistinctGenotype();

        // The cap falls in the middle of the tie: value ordering decides the first slot, insertion order the second.
        var result = Truncate(cap: 2, (tiedFirst, 0.1m), (dearest, 0.9m), (tiedSecond, 0.1m));

        result.GenotypeLikelihoods.Keys.Should().BeEquivalentTo(new[] { dearest.ToHlaNames(), tiedFirst.ToHlaNames() });
    }

    [Test]
    public void TruncateGenotypes_SumsTheKeptLikelihoodsInDescendingLikelihoodOrder()
    {
        var a = DistinctGenotype();
        var b = DistinctGenotype();
        var c = DistinctGenotype();

        var result = Truncate(cap: 3, (a, 0.1m), (b, 0.30m), (c, 0.200m));

        // decimal carries scale, so the sum's scale is the widest addend's - hence 0.600, three places, from 0.200.
        // Addition itself is exact until the 96-bit mantissa overflows, which likelihood magnitudes never approach, so
        // the order is not observable here. It is asserted below anyway, because the truncater builds the kept
        // dictionary in descending-likelihood order and sums it in that order, and that must not quietly change.
        result.SumOfLikelihoods.Should().Be(0.600m);
        result.SumOfLikelihoods.ToString().Should().Be("0.600");
        result.GenotypeLikelihoods.Values.Should().BeInDescendingOrder();
    }

    [Test]
    public void TruncateGenotypes_WhenTwoGenotypesShareOneNameKey_KeepsBothGenotypes()
    {
        // The collapse case: identical HLA names at different typing categories, which ToHlaNames() cannot tell apart.
        // Reachable only for a set holding more than one category, and every set in DEV holds SmallGGroup alone, so no
        // real donor exercises it today. It stays pinned because the pairing loop's indexer assignment relies on it.
        var name = fixture.Create<string>();
        var smallG = GenotypeAtA(new HlaAtKnownTypingCategory(name, HaplotypeTypingCategory.SmallGGroup));
        var gGroup = GenotypeAtA(new HlaAtKnownTypingCategory(name, HaplotypeTypingCategory.GGroup));
        var other = DistinctGenotype();

        var result = Truncate(cap: 1, (smallG, 0.5m), (gGroup, 0.5m), (other, 0.1m));

        // ONE surviving likelihood key, but TWO genotypes under it: truncation counts distinct names, and the genotype
        // filter is membership of the kept key set.
        result.GenotypeLikelihoods.Should().HaveCount(1);
        result.Genotypes.Should().BeEquivalentTo(new[] { smallG, gGroup });
        result.SumOfLikelihoods.Should().Be(0.5m);
    }

    [Test]
    public void TruncateGenotypes_WhenThereIsNothingToTruncate_ReturnsEmpty()
    {
        var result = Truncate(cap: 2000);

        result.Genotypes.Should().BeEmpty();
        result.GenotypeLikelihoods.Should().BeEmpty();
        result.SumOfLikelihoods.Should().Be(0);
    }

    /// <summary>
    /// Builds the truncater's two inputs the way <c>CompressedPhenotypeExpander</c>'s pairing loop builds them - same
    /// order, and the likelihood written through the indexer rather than <c>Add</c> so a collapsed key overwrites
    /// instead of throwing. This is the only place that has to change if the truncater's signature does, which keeps
    /// every assertion above an equivalence guard rather than a re-statement of the implementation.
    /// </summary>
    private static ImputedGenotypes Truncate(
        int cap,
        params (PhenotypeInfo<HlaAtKnownTypingCategory> Genotype, decimal Likelihood)[] keptPairs)
    {
        var haplotypes = new List<LociInfo<HlaAtKnownTypingCategory>>();
        var genotypePairs = new List<GenotypePair>();
        var genotypeNameKeys = new List<GenotypeNameKey>();
        var likelihoods = new Dictionary<GenotypeNameKey, decimal>();

        // The interning the pairing loop does, reproduced here rather than stubbed. An id per DISTINCT haplotype name
        // form is what makes the collapse case below land on one key without this helper arranging it.
        var idByName = new Dictionary<LociInfo<string>, int>();
        var haplotypeNamesById = new List<LociInfo<string>>();

        int IdOf(LociInfo<HlaAtKnownTypingCategory> haplotype)
        {
            var names = haplotype.Map(hla => hla?.Hla);

            if (!idByName.TryGetValue(names, out var id))
            {
                id = haplotypeNamesById.Count;
                idByName[names] = id;
                haplotypeNamesById.Add(names);
            }

            return id;
        }

        foreach (var (genotype, likelihood) in keptPairs)
        {
            // The truncater is handed pool indices rather than genotypes, so a fixture genotype is split into the two
            // haplotypes it would have been paired from. PhenotypeInfo equality is positional, so re-combining them
            // yields the same value - which is what the assertions above compare against.
            var position1 = genotype.ToLociInfo((_, p1, _) => p1);
            var position2 = genotype.ToLociInfo((_, _, p2) => p2);

            haplotypes.Add(position1);
            haplotypes.Add(position2);

            var nameKey = new GenotypeNameKey(IdOf(position1), IdOf(position2));

            genotypePairs.Add(new GenotypePair(haplotypes.Count - 2, haplotypes.Count - 1));
            genotypeNameKeys.Add(nameKey);
            likelihoods[nameKey] = likelihood;
        }

        var expanded = new ExpandedGenotypes(
            haplotypes, genotypePairs, genotypeNameKeys, likelihoods, haplotypeNamesById);

        return ExpandedGenotypeTruncater.TruncateGenotypes(likelihoods, expanded, cap);
    }

    /// <summary>A genotype no other genotype in the test can share a name with.</summary>
    private PhenotypeInfo<HlaAtKnownTypingCategory> DistinctGenotype() =>
        GenotypeAtA(new HlaAtKnownTypingCategory(fixture.Create<string>(), HaplotypeTypingCategory.SmallGGroup));

    /// <summary>
    /// Homozygous at A and untyped elsewhere. The truncater only ever reads a genotype's name form and its identity, so
    /// one typed locus is enough to make it distinguishable - and it keeps the tie fixtures readable.
    /// </summary>
    private static PhenotypeInfo<HlaAtKnownTypingCategory> GenotypeAtA(HlaAtKnownTypingCategory hla) =>
        new PhenotypeInfo<HlaAtKnownTypingCategory>().SetLocus(Locus.A, hla, hla);
}
