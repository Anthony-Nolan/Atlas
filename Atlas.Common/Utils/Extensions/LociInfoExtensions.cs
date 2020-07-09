using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;

namespace Atlas.Common.Utils.Extensions
{
    public static class LociInfoExtensions
    {
        public static LociInfo<Probability> ToProbabilities(this LociInfo<decimal?> decimals)
        {
            return decimals.Map(v => v.HasValue ? new Probability(v.Value) : null);
        }

        public static LociInfo<decimal?> ToDecimals(this LociInfo<Probability> decimals)
        {
            return decimals.Map(v => v?.Decimal);
        }
    }
}