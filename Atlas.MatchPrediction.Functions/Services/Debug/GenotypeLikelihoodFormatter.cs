using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Functions.Services.Debug
{
    public static class GenotypeLikelihoodFormatter
    {
        private const string FieldDelimiter = ",";

        public static string ToSingleDelimitedString(this Dictionary<PhenotypeInfo<string>, decimal> genotypeLikelihoods)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{BuildGenotypeLikelihoodHeader()}");

            foreach (var genotypeLikelihood in genotypeLikelihoods)
            {
                builder.AppendLine($"{genotypeLikelihood.Key.ToDelimitedString(genotypeLikelihood.Value)}");
            }

            return builder.ToString();
        }

        public static string ToSingleDelimitedString(this IEnumerable<GenotypeMatchDetails> genotypeMatchDetails)
        {
            // ReSharper disable once PossibleMultipleEnumeration - `IsNullOrEmpty` extension method does not enumerate full collection
            if (genotypeMatchDetails.IsNullOrEmpty())
            {
                return "No available genotype match details.";
            }

            var builder = new StringBuilder();

            var genotypePairs = BuildGenotypePairsWithMatchCounts(genotypeMatchDetails);

            foreach (var pair in genotypePairs)
            {
                builder.AppendLine(pair);
            }

            return builder.ToString();
        }

        private static IEnumerable<string> BuildGenotypePairsWithMatchCounts(IEnumerable<GenotypeMatchDetails> genotypeMatchDetails)
        {
            var header =  $"Total{FieldDelimiter}" +
                                $"A{FieldDelimiter}" +
                                $"B{FieldDelimiter}" +
                                $"C{FieldDelimiter}" +
                                $"DQB1{FieldDelimiter}" +
                                $"DRB1{FieldDelimiter}" +
                                $"{BuildGenotypeLikelihoodHeader("P-")}{FieldDelimiter}" +
                                $"{BuildGenotypeLikelihoodHeader("D-")}";

            var formattedStrings = new List<string> { header };
            formattedStrings.AddRange(
                genotypeMatchDetails
                .OrderByDescending(x => x.MatchCount)
                .Select(x =>
                    $"{BuildCounts(x.MatchCount, x.MatchCounts)}{FieldDelimiter}" +
                    $"{x.PatientGenotype.ToDelimitedString(x.PatientGenotypeLikelihood)}{FieldDelimiter}" +
                    $"{x.DonorGenotype.ToDelimitedString(x.DonorGenotypeLikelihood)}"));

            return formattedStrings;

            string BuildCounts(int totalCount, LociInfo<int?> locusCounts) =>
                $"{totalCount}{FieldDelimiter}" +
                $"{locusCounts.A}{FieldDelimiter}" +
                $"{locusCounts.B}{FieldDelimiter}" +
                $"{locusCounts.C}{FieldDelimiter}" +
                $"{locusCounts.Dqb1}{FieldDelimiter}" +
                $"{locusCounts.Drb1}";
        }

        private static string BuildGenotypeLikelihoodHeader(string fieldNamePrefix = null)
        {
            return $"{fieldNamePrefix}A_1{FieldDelimiter}" +
                   $"{fieldNamePrefix}A_2{FieldDelimiter}" +
                   $"{fieldNamePrefix}B_1{FieldDelimiter}" +
                   $"{fieldNamePrefix}B_2{FieldDelimiter}" +
                   $"{fieldNamePrefix}C_1{FieldDelimiter}" +
                   $"{fieldNamePrefix}C_2{FieldDelimiter}" +
                   $"{fieldNamePrefix}DQB1_1{FieldDelimiter}" +
                   $"{fieldNamePrefix}DQB1_2{FieldDelimiter}" +
                   $"{fieldNamePrefix}DRB1_1{FieldDelimiter}" +
                   $"{fieldNamePrefix}DRB1_2{FieldDelimiter}" +
                   $"{fieldNamePrefix}Likelihood";
        }

        private static string ToDelimitedString(this PhenotypeInfo<string> genotype, decimal likelihood)
        {
            return $"{genotype.A.Position1}{FieldDelimiter}" +
                   $"{genotype.A.Position2}{FieldDelimiter}" +
                   $"{genotype.B.Position1}{FieldDelimiter}" +
                   $"{genotype.B.Position2}{FieldDelimiter}" +
                   $"{genotype.C.Position1}{FieldDelimiter}" +
                   $"{genotype.C.Position2}{FieldDelimiter}" +
                   $"{genotype.Dqb1.Position1}{FieldDelimiter}" +
                   $"{genotype.Dqb1.Position2}{FieldDelimiter}" +
                   $"{genotype.Drb1.Position1}{FieldDelimiter}" +
                   $"{genotype.Drb1.Position2}{FieldDelimiter}" +
                   $"{likelihood}";
        }
    }
}
