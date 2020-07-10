using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction
{
    public class MatchProbabilityTestsBase
    {
        protected IMatchProbabilityService matchProbabilityService;
        protected IFrequencySetService importService;

        protected const string HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;

        protected static readonly string GGroupA1 = Alleles.UnambiguousAlleleDetails.A.Position1.GGroup;
        protected static readonly string GGroupA2 = Alleles.UnambiguousAlleleDetails.A.Position2.GGroup;
        protected static readonly string GGroupB1 = Alleles.UnambiguousAlleleDetails.B.Position1.GGroup;
        protected static readonly string GGroupB2 = Alleles.UnambiguousAlleleDetails.B.Position2.GGroup;
        protected static readonly string GGroupC1 = Alleles.UnambiguousAlleleDetails.C.Position1.GGroup;
        protected static readonly string GGroupC2 = Alleles.UnambiguousAlleleDetails.C.Position2.GGroup;
        protected static readonly string GGroupDqb11 = Alleles.UnambiguousAlleleDetails.Dqb1.Position1.GGroup;
        protected static readonly string GGroupDqb12 = Alleles.UnambiguousAlleleDetails.Dqb1.Position2.GGroup;
        protected static readonly string GGroupDrb11 = Alleles.UnambiguousAlleleDetails.Drb1.Position1.GGroup;
        protected static readonly string GGroupDrb12 = Alleles.UnambiguousAlleleDetails.Drb1.Position2.GGroup;
        
        protected readonly string DefaultRegistryCode = "default-registry-code";
        protected readonly string DefaultEthnicityCode = "default-ethnicity-code";

        [SetUp]
        protected void SetUp()
        {
            matchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>(); 
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
            
        }
        
        protected async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes, string ethnicityCode, string registryCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, haplotypes).Build();
            await importService.ImportFrequencySet(file);
        }

        protected static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());

        protected static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency1 => Builder<HaplotypeFrequency>.New
            .With(r => r.A, GGroupA1)
            .With(r => r.B, GGroupB1)
            .With(r => r.C, GGroupC1)
            .With(r => r.DQB1, GGroupDqb11)
            .With(r => r.DRB1, GGroupDrb11);

        protected Builder<HaplotypeFrequency> DefaultHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .With(r => r.A, GGroupA2)
            .With(r => r.B, GGroupB2)
            .With(r => r.C, GGroupC2)
            .With(r => r.DQB1, GGroupDqb12)
            .With(r => r.DRB1, GGroupDrb12);
    }
    
}