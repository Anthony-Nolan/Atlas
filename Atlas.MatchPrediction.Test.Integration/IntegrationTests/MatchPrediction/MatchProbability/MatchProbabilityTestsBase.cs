using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityTestsBase
    {
        protected IMatchProbabilityService MatchProbabilityService;
        protected IHaplotypeFrequencyService ImportService;

        protected const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        protected static readonly PhenotypeInfo<string> DefaultGGroups = Alleles.UnambiguousAlleleDetails.GGroups();
        
        protected const string DefaultRegistryCode = "default-registry-code";
        protected const string DefaultEthnicityCode = "default-ethnicity-code";

        [SetUp]
        protected void SetUp()
        {
            MatchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
            ImportService = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
        }

        protected async Task ImportFrequencies(
            IEnumerable<HaplotypeFrequency> haplotypes,
            string registryCode = DefaultRegistryCode,
            string ethnicityCode = DefaultEthnicityCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, haplotypes).Build();
            await ImportService.ImportFrequencySet(file);
        }

        protected static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency1 => HaplotypeFrequencyBuilder.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.GGroups().Split().Item1);

        protected static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .WithHaplotype(Alleles.UnambiguousAlleleDetails.GGroups().Split().Item2);

        protected static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());

        protected static Builder<MatchProbabilityInput> DefaultInputBuilder => Builder<MatchProbabilityInput>.New
            .With(i => i.HlaNomenclatureVersion, HlaNomenclatureVersion)
            .With(i => i.PatientHla, DefaultUnambiguousAllelesBuilder.Build())
            .With(i => i.DonorHla, DefaultUnambiguousAllelesBuilder.Build())
            .With(i => i.DonorFrequencySetMetadata, new FrequencySetMetadata
            {
                EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode
            })
            .With(i => i.PatientFrequencySetMetadata, new FrequencySetMetadata
            {
                EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode
            });
    }
}