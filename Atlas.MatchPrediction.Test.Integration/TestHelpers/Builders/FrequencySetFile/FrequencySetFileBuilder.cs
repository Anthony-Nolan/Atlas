using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Models.FrequencySetFile>;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile
{
    [Builder]
    internal static class FrequencySetFileBuilder
    {
        private const string CsvHeader = "a;b;c;dqb1;drb1;freq;nomenclature_version;population_id;don_pool;ethn";

        private const int DefaultPopulationId = 1;
        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

        internal static Builder FileWithoutContents()
        {
            var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.csv";

            return Builder.New.With(x => x.FileName, uniqueFileName);
        }

        internal static Builder New(
            string registryCode = null, 
            string ethnicityCode = null,
            string nomenclatureVersion = HlaNomenclatureVersion,
            int populationId = DefaultPopulationId,
            int haplotypeCount = 1,
            decimal frequencyValue = 0.00001m)
        {
            return FileWithoutContents()
                .With(x => x.Contents, 
                    GetStream(BuildCsvFile(haplotypeCount, frequencyValue, registryCode, ethnicityCode, populationId, nomenclatureVersion)));
        }

        internal static Builder New(
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
            string registryCode = null,
            string ethnicityCode = null,
            string nomenclatureVersion = HlaNomenclatureVersion,
            int populationId = DefaultPopulationId)
        {
            return FileWithoutContents()
                .WithHaplotypeFrequencies(registryCode, ethnicityCode, populationId, nomenclatureVersion, haplotypeFrequencies);
        }

        internal static Builder New(IEnumerable<(HaplotypeFrequency, HaplotypeFrequencySet)> haplotypeFrequencies)
        {
            return FileWithoutContents().WithHaplotypeFrequencies(haplotypeFrequencies);
        }

        internal static Builder WithHaplotypeFrequencies(this Builder builder, IEnumerable<(HaplotypeFrequency, HaplotypeFrequencySet)> haplotypeFrequencies)
        {
            return builder.With(x => 
                x.Contents,
                GetStream(BuildCsvFile(haplotypeFrequencies)));
        }

        internal static Builder WithHaplotypeFrequencies(
            this Builder builder,
            string registryCode,
            string ethnicityCode,
            int populationId,
            string nomenclatureVersion, 
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            return builder.With(x =>
                    x.Contents,
                GetStream(BuildCsvFile(registryCode, ethnicityCode, populationId, nomenclatureVersion, haplotypeFrequencies)));
        }

        internal static Builder WithHaplotypeFrequencyFileStream(this Builder builder, Stream stream)
        {
            return builder.With(f => f.Contents, stream);
        }

        internal static Builder WithInvalidCsvFormat(
            string registryCode = null,
            string ethnicityCode = null,
            int populationId = DefaultPopulationId,
            string nomenclatureVersion = HlaNomenclatureVersion,
            int haplotypeCount = 1,
            decimal frequencyValue = 0.00001m)
        {
            var csvString = BuildCsvFile(haplotypeCount, frequencyValue, registryCode, ethnicityCode, populationId, nomenclatureVersion);

            return FileWithoutContents()
                .With(x => x.Contents, GetStream(csvString.Substring(csvString.Length - 10)));
        }

        private static string BuildCsvFile(
            int frequencyCount,
            decimal frequencyValue, 
            string registryCode, 
            string ethnicityCode,
            int populationId,
            string nomenclatureVersion)
        {
            var file = new StringBuilder(CsvHeader + Environment.NewLine);

            var validHaplotypes = new AmbiguousPhenotypeExpander()
                .LazilyExpandPhenotype(AlleleGroups.GGroups.ToPhenotypeInfo((_, x) => x));

            using (var enumerator = validHaplotypes.GetEnumerator())
            {
                for (var i = 0; i < frequencyCount; i++)
                {
                    enumerator.MoveNext();
                    var haplotype = enumerator.Current ?? new PhenotypeInfo<string>();
                    var csvFileBodySingleFrequency = $"{haplotype.A.Position1};" +
                                                     $"{haplotype.B.Position1};" +
                                                     $"{haplotype.C.Position1};" +
                                                     $"{haplotype.Dqb1.Position1};" +
                                                     $"{haplotype.Drb1.Position1};" +
                                                     $"{frequencyValue};" +
                                                     $"{nomenclatureVersion};" +
                                                     $"{populationId};" +
                                                     $"{registryCode};" +
                                                     $"{ethnicityCode}";
                    file.AppendLine(csvFileBodySingleFrequency);
                }

                return file.ToString();
            }
        }

        private static string BuildCsvFile(
            string registryCode,
            string ethnicityCode,
            int populationId,
            string nomenclatureVersion,
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            var csvFileBodyFrequencies = haplotypeFrequencies
                .Select(h =>
                    $"{h.A};{h.B};{h.C};{h.DQB1};{h.DRB1};{h.Frequency};{nomenclatureVersion};{populationId};{registryCode};{ethnicityCode};")
                .ToList();

            return ToFile(csvFileBodyFrequencies);
        }

        private static string BuildCsvFile(IEnumerable<(HaplotypeFrequency, HaplotypeFrequencySet)> haplotypeFrequencies)
        {
            var csvFileBodyFrequencies = haplotypeFrequencies
                .Select(h => 
                    $"{h.Item1.A};" +
                    $"{h.Item1.B};" +
                    $"{h.Item1.C};" +
                    $"{h.Item1.DQB1};" +
                    $"{h.Item1.DRB1};" +
                    $"{h.Item1.Frequency};" +
                    $"{h.Item2.HlaNomenclatureVersion};" +
                    $"{h.Item2.PopulationId};" +
                    $"{h.Item2.RegistryCode};" +
                    $"{h.Item2.EthnicityCode}")
                .ToList();

            return ToFile(csvFileBodyFrequencies);
        }

        private static string ToFile(IEnumerable<string> csvFileBodyFrequencies)
        {
            var file = new StringBuilder(CsvHeader + Environment.NewLine);

            foreach (var csvFileBodyFrequency in csvFileBodyFrequencies)
            {
                file.AppendLine(csvFileBodyFrequency);
            }

            return file.ToString();
        }

        private static Stream GetStream(string fileContents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
        }
    }
}