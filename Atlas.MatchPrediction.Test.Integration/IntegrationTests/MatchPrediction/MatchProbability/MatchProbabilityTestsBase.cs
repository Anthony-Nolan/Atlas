using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityTestsBase
    {
        protected readonly FrequencySetMetadata GlobalHfSetMetadata = new() { EthnicityCode = null, RegistryCode = null };

        protected IMatchProbabilityService MatchProbabilityService;
        protected IHaplotypeFrequencyService ImportService;

        protected const string HfSetHlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;
        protected const string MatchingAlgorithmHlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

        protected static readonly PhenotypeInfo<string> DefaultGGroups = Alleles.UnambiguousAlleleDetails.GGroups();
        protected static readonly PhenotypeInfo<string> DefaultSmallGGroups = Alleles.UnambiguousAlleleDetails.SmallGGroups();

        protected const string DefaultRegistryCode = "default-registry-code";
        protected const string DefaultEthnicityCode = "default-ethnicity-code";

        protected const decimal DefaultHaplotypeFrequency = 0.00001m;

        [SetUp]
        protected void SetUp()
        {
            MatchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
            ImportService = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
        }

        protected async Task ImportFrequencies(
            IEnumerable<HaplotypeFrequency> haplotypes,
            string registryCode = DefaultRegistryCode,
            string ethnicityCode = DefaultEthnicityCode,
            string nomenclatureVersion = HfSetHlaNomenclatureVersion,
            ImportTypingCategory typingCategory = ImportTypingCategory.LargeGGroup)
        {
            var registry = registryCode == null ? null : new[] { registryCode };
            var ethnicity = ethnicityCode == null ? null : new[] { ethnicityCode };

            using var file = FrequencySetFileBuilder
                .New(haplotypes, registry, ethnicity, nomenclatureVersion: nomenclatureVersion, typingCategory: typingCategory)
                .Build();
            await ImportService.ImportFrequencySet(file);
        }

        protected static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency1 => HaplotypeFrequencyBuilder.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.GGroups().Split().Item1);

        protected static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.GGroups().Split().Item2);

        protected static Builder<HaplotypeFrequency> DefaultSmallGGroupHaplotypeFrequency1 => HaplotypeFrequencyBuilder.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.SmallGGroups().Split().Item1);

        protected static Builder<HaplotypeFrequency> DefaultSmallGGroupHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.SmallGGroups().Split().Item2);

        protected static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder => new(Alleles.UnambiguousAlleleDetails.Alleles());

        protected static Builder<SingleDonorMatchProbabilityInput> DefaultInputBuilder => SingleDonorMatchProbabilityInputBuilder.Default
            .With(x => x.MatchingAlgorithmHlaNomenclatureVersion, MatchingAlgorithmHlaNomenclatureVersion)
            .WithPatientHla(DefaultUnambiguousAllelesBuilder.Build())
            .WithDonorHla(DefaultUnambiguousAllelesBuilder.Build())
            .WithDonorMetadata(new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode })
            .WithPatientMetadata(new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode });
    }
}