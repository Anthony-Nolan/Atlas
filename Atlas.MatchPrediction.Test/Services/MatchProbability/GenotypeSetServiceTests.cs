using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

[TestFixture]
internal class GenotypeSetServiceTests
{
    private IGenotypeImputationService genotypeImputer;
    private IGenotypeConverter genotypeConverter;
    private IHaplotypeFrequencyService haplotypeFrequencyService;

    private GenotypeSetService sut;

    [SetUp]
    public void SetUp()
    {
        genotypeImputer = Substitute.For<IGenotypeImputationService>();
        genotypeConverter = Substitute.For<IGenotypeConverter>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();

        sut = new GenotypeSetService(genotypeImputer, genotypeConverter, haplotypeFrequencyService);
    }

    [Test]
    public async Task GetGenotypeSet_WhenGenotypesEmpty_ReturnsUnrepresentedResult()
    {
        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>());

        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());
        genotypeImputer.Impute(null).ReturnsForAnyArgs(ImputedGenotypes.Empty());

        var subjectData = BuildDefaultSubjectData();
        var parameters = new MatchPredictionParameters(new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 });

        var result = await sut.GetGenotypeSet(subjectData, parameters);

        result.IsUnrepresented.Should().BeTrue();
        result.Genotypes.Should().BeEmpty();
        result.SumOfLikelihoods.Should().Be(0m);
    }

    [Test]
    public async Task GetGenotypeSet_WhenGenotypesExist_ReturnsConvertedGenotypes()
    {
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());
        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);

        var convertedGenotypes = new List<GenotypeAtDesiredResolutions>
        {
            new GenotypeAtDesiredResolutionsBuilder().Default().Build()
        };
        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(convertedGenotypes);

        var subjectData = BuildDefaultSubjectData();
        var parameters = new MatchPredictionParameters(new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 });

        var result = await sut.GetGenotypeSet(subjectData, parameters);

        result.IsUnrepresented.Should().BeFalse();
        result.Genotypes.Should().BeEquivalentTo(convertedGenotypes);
        result.SumOfLikelihoods.Should().Be(imputedGenotypes.SumOfLikelihoods);
    }

    [Test]
    public async Task GetGenotypeSet_WithNullNomenclatureVersion_DoesNotApplyReplacements()
    {
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());
        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);

        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>
                {
                    new GenotypeAtDesiredResolutionsBuilder().Default().Build()
                }
            );

        var subjectData = BuildDefaultSubjectData();
        var parameters = new MatchPredictionParameters(
            new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 }
        );

        await sut.GetGenotypeSet(subjectData, parameters);

        await genotypeImputer.Received(1).Impute(Arg.Is<ImputationInput>(i =>
                i.SubjectData == subjectData
            )
        );
    }

    [Test]
    public async Task GetGenotypeSet_WithValidNomenclatureVersion_AppliesReplacementMapping()
    {
        var hlaTyping = new PhenotypeInfo<string>(
            valueA: new LocusInfo<string>("*01:01", "*02:01"),
            valueB: new LocusInfo<string>("*07:02", "*08:01"),
            valueC: new LocusInfo<string>("*04:09N", "*07:01"),
            valueDqb1: new LocusInfo<string>("*03:01", "*05:01"),
            valueDrb1: new LocusInfo<string>("*04:01", "*07:01")
        );

        var subjectData = new SubjectData(hlaTyping, new SubjectFrequencySet(new HaplotypeFrequencySet(), "test"));
        var parameters = new MatchPredictionParameters(
            new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Drb1 },
            "3590"
        );

        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);
        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>
                {
                    new GenotypeAtDesiredResolutionsBuilder().Default().Build()
                }
            );

        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());
        await sut.GetGenotypeSet(subjectData, parameters);

        // Verify that the replacement was applied: C locus "*04:09N" should become "*04:09L"
        await genotypeImputer.Received(1).Impute(Arg.Is<ImputationInput>(i =>
                i.SubjectData.HlaTyping.GetPosition(Locus.C, LocusPosition.One) == "*04:09L"
            )
        );
    }

    [Test]
    public async Task GetGenotypeSet_WithNomenclatureVersionBeforeMapping_DoesNotApplyReplacement()
    {
        var hlaTyping = new PhenotypeInfo<string>(
            valueA: new LocusInfo<string>("*01:01", "*02:01"),
            valueB: new LocusInfo<string>("*07:02", "*08:01"),
            valueC: new LocusInfo<string>("*04:09N", "*07:01"),
            valueDqb1: new LocusInfo<string>("*03:01", "*05:01"),
            valueDrb1: new LocusInfo<string>("*04:01", "*07:01")
        );

        var subjectData = new SubjectData(hlaTyping, new SubjectFrequencySet(new HaplotypeFrequencySet(), "test"));
        // Version 3589 is before the mapping at 3590, so no replacement
        var parameters = new MatchPredictionParameters(
            new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Drb1 },
            "3589"
        );

        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);
        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>
                {
                    new GenotypeAtDesiredResolutionsBuilder().Default().Build()
                }
            );
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());

        await sut.GetGenotypeSet(subjectData, parameters);

        // The original value should be preserved (no replacement applied)
        await genotypeImputer.Received(1).Impute(Arg.Is<ImputationInput>(i =>
                i.SubjectData.HlaTyping.GetPosition(Locus.C, LocusPosition.One) == "*04:09N"
            )
        );
    }

    [Test]
    public void GetGenotypeSet_WithInvalidNomenclatureVersion_ThrowsArgumentException()
    {
        var subjectData = BuildDefaultSubjectData();
        var parameters = new MatchPredictionParameters(
            new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 },
            "not-a-number"
        );

        var act = async () => await sut.GetGenotypeSet(subjectData, parameters);

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be a valid integer*");
    }

    [Test]
    public async Task GetPatientGenotypeSet_CallsImputerWithPatientData()
    {
        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);

        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>
                {
                    new GenotypeAtDesiredResolutionsBuilder().Default().Build()
                }
            );
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(new HaplotypeFrequencySet());

        var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();

        await sut.GetPatientGenotypeSet(input);

        await genotypeImputer.Received(1).Impute(Arg.Any<ImputationInput>());
    }

    [Test]
    public async Task GetPatientGenotypeSet_UsesPatientFrequencySetFromHaplotypeFrequencyService()
    {
        var expectedFrequencySet = new HaplotypeFrequencySet { HlaNomenclatureVersion = "3590" };
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(null)
            .ReturnsForAnyArgs(expectedFrequencySet);

        var imputedGenotypes = new ImputedGenotypesBuilder().Default().Build();
        genotypeImputer.Impute(null).ReturnsForAnyArgs(imputedGenotypes);

        genotypeConverter.ConvertGenotypesForMatchCalculation(null)
            .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions>
                {
                    new GenotypeAtDesiredResolutionsBuilder().Default().Build()
                }
            );

        var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();

        await sut.GetPatientGenotypeSet(input);

        await genotypeImputer.Received(1).Impute(Arg.Is<ImputationInput>(i =>
                i.SubjectData.SubjectFrequencySet.FrequencySet == expectedFrequencySet
            )
        );
    }

    private static SubjectData BuildDefaultSubjectData()
    {
        var hlaTyping = new PhenotypeInfoBuilder<string>("hla").Build();
        var frequencySet = new SubjectFrequencySet(new HaplotypeFrequencySet(), "subject");
        return new SubjectData(hlaTyping, frequencySet);
    }
}