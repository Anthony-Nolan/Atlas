using System.Collections.Generic;
using Atlas.Common.GeneticData;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.MatchingAlgorithm.Data.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global - instantiated by Dapper
    internal class DonorLocusMatch
    {
        public int DonorId { get; set; }
        
        // ReSharper disable once MemberCanBePrivate.Global - needed for Dapper deserialisation
        public int? TypePosition1 { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global - needed for Dapper deserialisation
        public int? TypePosition2 { get; set; }

        internal async IAsyncEnumerable<PotentialHlaMatchRelation> ToPotentialHlaMatchRelations(Locus locus)
        {
            if (TypePosition1 != null)
            {
                yield return new PotentialHlaMatchRelation
                {
                    Locus = locus,
                    Name = "Unknown",
                    SearchTypePosition = TypePosition.One.ToLocusPosition(),
                    MatchingTypePosition = ((TypePosition) TypePosition1).ToLocusPosition(),
                    DonorId = DonorId
                };
            }

            if (TypePosition2 != null)
            {
                yield return new PotentialHlaMatchRelation
                {
                    Locus = locus,
                    Name = "Unknown",
                    SearchTypePosition = TypePosition.Two.ToLocusPosition(),
                    MatchingTypePosition = ((TypePosition) TypePosition2).ToLocusPosition(),
                    DonorId = DonorId
                };
            }
        }
    }
}