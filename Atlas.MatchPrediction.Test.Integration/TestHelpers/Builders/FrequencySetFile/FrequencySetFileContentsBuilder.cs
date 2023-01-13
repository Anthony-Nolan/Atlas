using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Test.Integration.TestHelpers.Models.SerialisableFrequencySetFileContents>;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile
{
    [Builder]
    internal static class FrequencySetFileContentsBuilder
    {
        private const int DefaultPopulationId = 1;
        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

        internal static Builder Default => Builder.New
            .With(f => f.nomenclatureVersion, HlaNomenclatureVersion)
            .With(f => f.populationId, DefaultPopulationId);


        internal static Builder NewWithFrequencyCount(
            string[] ethnicity = null,
            string[] registries = null,
            int haplotypeCount = 1,
            decimal frequencyValue = 0.00001m,
            ImportTypingCategory typingCategory = default)
        {
            return Default
                .With(f => f.ethn, ethnicity)
                .With(f => f.donPool, registries)
                .With(f => f.TypingCategory, typingCategory)
                .With(x => x.frequencies, CreateFrequencyRecords(haplotypeCount, frequencyValue));
        }

        internal static Builder NewWithFrequencies(
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
            string[] ethnicity = null,
            string[] registries = null,
            ImportTypingCategory typingCategory = default)
        {
            return Default
                .With(f => f.ethn, ethnicity)
                .With(f => f.donPool, registries)
                .With(f => f.TypingCategory, typingCategory)
                .With(x => x.frequencies, CreateFrequencyRecords(haplotypeFrequencies));
        }

        internal static Builder WithTypingCategory(this Builder builder, ImportTypingCategory typingCategory) =>
            builder.With(f => f.TypingCategory, typingCategory);

        private static IEnumerable<FrequencyRecord> CreateFrequencyRecords(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            return haplotypeFrequencies
                .Select(h => new FrequencyRecord
                {
                    A = h.A,
                    B = h.B,
                    C = h.C,
                    Dqb1 = h.DQB1,
                    Drb1 = h.DRB1,
                    Frequency = h.Frequency
                });
        }

        private static IEnumerable<FrequencyRecord> CreateFrequencyRecords(int haplotypeCount, decimal frequencyValue)
        {
            var validHaplotypes = new AmbiguousPhenotypeExpander()
                .LazilyExpandPhenotype(AlleleGroups.GGroups.ToPhenotypeInfo((_, x) => x));

            using (var enumerator = validHaplotypes.GetEnumerator())
            {
                for (var i = 0; i < haplotypeCount; i++)
                {
                    enumerator.MoveNext();
                    var haplotype = enumerator.Current ?? new PhenotypeInfo<string>();
                    var frequencyRecord = new FrequencyRecord
                    {
                        A = haplotype.A.Position1,
                        B = haplotype.B.Position1,
                        C = haplotype.C.Position1,
                        Dqb1 = haplotype.Dqb1.Position1,
                        Drb1 = haplotype.Drb1.Position1,
                        Frequency = frequencyValue
                    };

                    yield return frequencyRecord;
                }
            }
        }
    }
}