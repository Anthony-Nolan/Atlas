using System.Collections.Generic;
using System.Text;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Functions.Services.Debug
{
    public static class GenotypeSetFormatter
    {
        private const string GenotypeHeaderFields = "A_1;A_2;B_1;B_2;C_1;C_2;DQB1_1;DQB1_2;DRB1_1;DRB1_2";
        private const string FieldDelimiter = ";";

        public static string ToSingleDelimitedString(this Dictionary<PhenotypeInfo<string>, decimal> genotypeLikelihoods)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{GenotypeHeaderFields}{FieldDelimiter}Likelihood");

            foreach (var genotypeLikelihood in genotypeLikelihoods)
            {
                builder.AppendLine($"{genotypeLikelihood.Key.ToDelimitedString()}{FieldDelimiter}{genotypeLikelihood.Value}");
            }

            return builder.ToString();
        }

        private static string ToDelimitedString(this PhenotypeInfo<string> genotype)
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
                   $"{genotype.Drb1.Position2}";
        }
    }
}
