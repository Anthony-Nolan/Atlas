using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction
{
    public class MatchProbabilityTestsBase
    {
        protected IMatchProbabilityService matchProbabilityService;
        protected IHaplotypeFrequencyService importService;

        protected const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        protected static readonly PhenotypeInfo<string> DefaultGGroups = Alleles.UnambiguousAlleleDetails.GGroups();
        
        protected const string DefaultRegistryCode = "default-registry-code";
        protected const string DefaultEthnicityCode = "default-ethnicity-code";

        [SetUp]
        protected void SetUp()
        {
            matchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
        }

        protected async Task ImportFrequencies(
            IEnumerable<HaplotypeFrequency> haplotypes,
            string registryCode = DefaultRegistryCode,
            string ethnicityCode = DefaultEthnicityCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, haplotypes).Build();
            await importService.ImportFrequencySet(file);
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