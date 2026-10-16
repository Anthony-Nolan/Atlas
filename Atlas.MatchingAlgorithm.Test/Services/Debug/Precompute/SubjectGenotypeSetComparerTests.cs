using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchPrediction.Models;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Debug.Precompute;

[TestFixture]
internal class SubjectGenotypeSetComparerTests
{
    private const int NoLimit = int.MaxValue;

    private Fixture fixture;
    private int frequencySetId;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        frequencySetId = fixture.Create<int>();
    }

    [Test]
    public void Compare_TheSameGenotypesInAnotherOrder_GivesNoDifference()
    {
        var genotypes = Genotypes(4);
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes, sum), Set(genotypes.AsEnumerable().Reverse(), sum), NoLimit);

        comparison.DifferenceCount.Should().Be(0);
        comparison.Differences.Should().BeEmpty();
    }

    [Test]
    public void Compare_CopiesOfTheSameGenotypes_GivesNoDifference()
    {
        // A decoded set has new objects with the same values.
        var genotypes = Genotypes(3);
        var copies = genotypes.Select(genotype => Genotype(
            genotype.StringMatchableResolution.Map(name => name),
            genotype.HaplotypeResolution.Map(name => name),
            genotype.GenotypeLikelihood));
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes, sum), Set(copies, sum), NoLimit);

        comparison.DifferenceCount.Should().Be(0);
    }

    [Test]
    public void Compare_WithOneChangedLikelihood_GivesThatDifference()
    {
        var genotypes = Genotypes(3);
        var changed = genotypes[1];
        var storedLikelihood = fixture.Create<decimal>();
        var stored = genotypes.Select(genotype => genotype == changed ? WithLikelihood(genotype, storedLikelihood) : genotype);
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes, sum), Set(stored, sum), NoLimit);

        comparison.DifferenceCount.Should().Be(1);
        comparison.Differences.Should().ContainSingle().Which.Should().BeEquivalentTo(new GenotypeSetDifference(
            GenotypeSetDifferenceKind.GenotypeLikelihood,
            changed.StringMatchableResolution,
            changed.HaplotypeResolution,
            Text(changed.GenotypeLikelihood),
            Text(storedLikelihood)));
    }

    [Test]
    public void Compare_WithAGenotypeInTheLiveSetOnly_GivesTheCountAndTheGenotype()
    {
        var genotypes = Genotypes(3);
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes, sum), Set(genotypes.Take(2), sum), NoLimit);

        comparison.Differences.Should().Equal(
            new GenotypeSetDifference(GenotypeSetDifferenceKind.GenotypeCount, null, null, Text(3), Text(2)),
            new GenotypeSetDifference(
                GenotypeSetDifferenceKind.GenotypeOnlyInLive,
                genotypes[2].StringMatchableResolution,
                genotypes[2].HaplotypeResolution,
                Text(genotypes[2].GenotypeLikelihood),
                null));
    }

    [Test]
    public void Compare_WithAGenotypeInTheStoredSetOnly_GivesTheCountAndTheGenotype()
    {
        var genotypes = Genotypes(3);
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes.Skip(1), sum), Set(genotypes, sum), NoLimit);

        comparison.Differences.Should().Equal(
            new GenotypeSetDifference(GenotypeSetDifferenceKind.GenotypeCount, null, null, Text(2), Text(3)),
            new GenotypeSetDifference(
                GenotypeSetDifferenceKind.GenotypeOnlyInStored,
                genotypes[0].StringMatchableResolution,
                genotypes[0].HaplotypeResolution,
                null,
                Text(genotypes[0].GenotypeLikelihood)));
    }

    [Test]
    public void Compare_ARepeatedGenotype_ComparesItsLikelihoodsInSortedOrder()
    {
        // Two genotypes that differ only in typing category have the same names.
        var genotype = Genotypes(1).Single();
        var otherLikelihood = fixture.Create<decimal>();
        var live = new[] { genotype, WithLikelihood(genotype, otherLikelihood) };
        var stored = new[] { WithLikelihood(genotype, otherLikelihood), genotype };
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set(live, sum), Set(stored, sum), NoLimit);

        comparison.DifferenceCount.Should().Be(0);
    }

    [Test]
    public void Compare_AGenotypeRepeatedInTheLiveSetOnly_GivesTheExtraOneAsOnlyInLive()
    {
        var genotype = Genotypes(1).Single();
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set([genotype, genotype], sum), Set([genotype], sum), NoLimit);

        comparison.Differences.Select(difference => difference.Kind).Should().Equal(
            GenotypeSetDifferenceKind.GenotypeCount,
            GenotypeSetDifferenceKind.GenotypeOnlyInLive);
    }

    [Test]
    public void Compare_WithSwappedPositionsAtALocus_GivesADifference()
    {
        // The engine is deterministic, so the same typing gives the same positions.
        var genotype = Genotypes(1).Single();
        var locusA = genotype.StringMatchableResolution.A;
        var swapped = Genotype(
            genotype.StringMatchableResolution.SetLocus(Locus.A, locusA.Position2, locusA.Position1),
            genotype.HaplotypeResolution,
            genotype.GenotypeLikelihood);
        var sum = fixture.Create<decimal>();

        var comparison = Compare(Set([genotype], sum), Set([swapped], sum), NoLimit);

        comparison.Differences.Select(difference => difference.Kind).Should().BeEquivalentTo(new[]
        {
            GenotypeSetDifferenceKind.GenotypeOnlyInLive,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored
        });
    }

    [Test]
    public void Compare_WithADifferentSum_GivesTheSum()
    {
        var genotypes = Genotypes(2);
        var liveSum = fixture.Create<decimal>();
        var storedSum = fixture.Create<decimal>();

        var comparison = Compare(Set(genotypes, liveSum), Set(genotypes, storedSum), NoLimit);

        comparison.Differences.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new GenotypeSetDifference(GenotypeSetDifferenceKind.SumOfLikelihoods, null, null, Text(liveSum), Text(storedSum)));
    }

    [Test]
    public void Compare_AnUnrepresentedSetWithARepresentedSet_GivesTheSetDifferencesFirst_ThenEachGenotype()
    {
        var stored = Set(Genotypes(3), fixture.Create<decimal>());

        var comparison = Compare(new SubjectGenotypeSet(true, [], 0m), stored, NoLimit);

        comparison.DifferenceCount.Should().Be(6);
        comparison.Differences.Select(difference => difference.Kind).Should().Equal(
            GenotypeSetDifferenceKind.IsUnrepresented,
            GenotypeSetDifferenceKind.GenotypeCount,
            GenotypeSetDifferenceKind.SumOfLikelihoods,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored);
        comparison.Differences[0].Should().BeEquivalentTo(
            new GenotypeSetDifference(GenotypeSetDifferenceKind.IsUnrepresented, null, null, "true", "false"));
    }

    [Test]
    public void Compare_DecimalsOfTheSameValueWithAnotherScale_GivesNoDifference()
    {
        // 1.10 and 1.1 give the same probabilities.
        var genotypes = Genotypes(2);
        var withMoreDecimalPlaces = genotypes.Select(genotype => WithLikelihood(genotype, genotype.GenotypeLikelihood * 1.00m)).ToList();
        var sum = fixture.Create<decimal>();
        Text(withMoreDecimalPlaces[0].GenotypeLikelihood).Should().NotBe(Text(genotypes[0].GenotypeLikelihood), "the scale must differ");

        var comparison = Compare(Set(genotypes, sum), Set(withMoreDecimalPlaces, sum * 1.000m), NoLimit);

        comparison.DifferenceCount.Should().Be(0);
    }

    [Test]
    public void Compare_WithMoreDifferencesThanTheLimit_GivesTheFirstOnes_AndCountsThemAll()
    {
        var genotypes = Genotypes(5);
        var stored = genotypes.Select(genotype => WithLikelihood(genotype, fixture.Create<decimal>())).ToList();
        var sum = fixture.Create<decimal>();
        var all = Compare(Set(genotypes, sum), Set(stored, sum), NoLimit);

        var comparison = Compare(Set(genotypes, sum), Set(stored, sum), 2);

        comparison.DifferenceCount.Should().Be(5);
        comparison.Differences.Should().Equal(all.Differences.Take(2));
    }

    [Test]
    public void Compare_GivesTheGenotypeDifferencesInTheSameOrder_WhateverTheOrderOfTheGenotypes()
    {
        var genotypes = Genotypes(4);
        var stored = genotypes.Select(genotype => WithLikelihood(genotype, fixture.Create<decimal>())).ToList();
        var sum = fixture.Create<decimal>();

        var inOrder = Compare(Set(genotypes, sum), Set(stored, sum), NoLimit);
        var reversed = Compare(
            Set(genotypes.AsEnumerable().Reverse(), sum),
            Set(stored.AsEnumerable().Reverse(), sum),
            NoLimit);

        reversed.Differences.Should().Equal(inOrder.Differences);
    }

    [Test]
    public void Compare_TheSameGenotypesWithAnotherFrequencySet_GivesTheTwoFrequencySets()
    {
        var genotypes = Genotypes(2);
        var sum = fixture.Create<decimal>();
        var storedFrequencySetId = fixture.Create<int>();

        var comparison = SubjectGenotypeSetComparer.Compare(Set(genotypes, sum), frequencySetId, Set(genotypes, sum), storedFrequencySetId, NoLimit);

        comparison.DifferenceCount.Should().Be(1);
        comparison.Differences.Should().ContainSingle().Which.Should().BeEquivalentTo(new GenotypeSetDifference(
            GenotypeSetDifferenceKind.HaplotypeFrequencySet,
            null,
            null,
            Text(frequencySetId),
            Text(storedFrequencySetId)));
    }

    [Test]
    public void Compare_WithAnotherFrequencySet_GivesTheFrequencySetsBeforeTheOtherDifferences()
    {
        // The other differences come from the two sets, so the difference of the sets tells why they are there.
        var stored = Set(Genotypes(2), fixture.Create<decimal>());

        var comparison = SubjectGenotypeSetComparer.Compare(
            new SubjectGenotypeSet(true, [], 0m),
            frequencySetId,
            stored,
            fixture.Create<int>(),
            NoLimit);

        comparison.Differences.Select(difference => difference.Kind).Should().Equal(
            GenotypeSetDifferenceKind.HaplotypeFrequencySet,
            GenotypeSetDifferenceKind.IsUnrepresented,
            GenotypeSetDifferenceKind.GenotypeCount,
            GenotypeSetDifferenceKind.SumOfLikelihoods,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored,
            GenotypeSetDifferenceKind.GenotypeOnlyInStored);
    }

    /// <summary>Compares two sets of one frequency set, as for most stored rows.</summary>
    private GenotypeSetComparison Compare(SubjectGenotypeSet live, SubjectGenotypeSet stored, int maxDifferences) =>
        SubjectGenotypeSetComparer.Compare(live, frequencySetId, stored, frequencySetId, maxDifferences);

    private static SubjectGenotypeSet Set(IEnumerable<GenotypeAtDesiredResolutions> genotypes, decimal sum) =>
        new(false, genotypes.ToList(), sum);

    /// <summary>Genotypes with resolutions and likelihoods of their own.</summary>
    private List<GenotypeAtDesiredResolutions> Genotypes(int count) =>
        Enumerable.Range(0, count).Select(_ => Genotype(Typing(), Typing(), fixture.Create<decimal>())).ToList();

    private static GenotypeAtDesiredResolutions Genotype(
        PhenotypeInfo<string> stringMatchableResolution,
        PhenotypeInfo<string> haplotypeResolution,
        decimal likelihood) =>
        new(new ImputedGenotype(null, haplotypeResolution, likelihood), stringMatchableResolution);

    private static GenotypeAtDesiredResolutions WithLikelihood(GenotypeAtDesiredResolutions genotype, decimal likelihood) =>
        Genotype(genotype.StringMatchableResolution, genotype.HaplotypeResolution, likelihood);

    private PhenotypeInfo<string> Typing()
    {
        var builder = new PhenotypeInfoBuilder<string>();
        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            builder = builder.WithDataAt(locus, fixture.Create<string>(), fixture.Create<string>());
        }

        return builder.Build();
    }

    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
