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

        /// <summary>
        /// Converts a collection of GenotypeMatchDetails to a collection of formatted strings.
        /// Uses yield return to avoid creating a large collection in memory.
        /// </summary>
        public static IEnumerable<string> ToFormattedStrings(this IEnumerable<GenotypeMatchDetails> genotypeMatchDetails)
        {
            // ReSharper disable once PossibleMultipleEnumeration - `IsNullOrEmpty` extension method does not enumerate full collection
            if (genotypeMatchDetails.IsNullOrEmpty())
            {
                yield break;
            }

            yield return $"Total{FieldDelimiter}" +
                         $"A{FieldDelimiter}" +
                         $"B{FieldDelimiter}" +
                         $"C{FieldDelimiter}" +
                         $"DQB1{FieldDelimiter}" +
                         $"DRB1{FieldDelimiter}" +
                         $"{BuildGenotypeLikelihoodHeader("P-")}{FieldDelimiter}" +
                         $"{BuildGenotypeLikelihoodHeader("D-")}";

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var details in genotypeMatchDetails.OrderByDescending(x => x.MatchCount))
            {
                yield return $"{BuildCounts(details.MatchCount, details.MatchCounts)}{FieldDelimiter}" +
                             $"{details.PatientGenotype.ToDelimitedString(details.PatientGenotypeLikelihood)}{FieldDelimiter}" +
                             $"{details.DonorGenotype.ToDelimitedString(details.DonorGenotypeLikelihood)}";
            }

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
