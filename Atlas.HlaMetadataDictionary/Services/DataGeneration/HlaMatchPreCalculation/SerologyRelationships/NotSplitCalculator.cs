using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships
{
    internal class NotSplitCalculator: MatchingSerologyCalculatorBase
    {
        public NotSplitCalculator(IEnumerable<RelSerSer> serologyRelationships) :
            base(serologyRelationships)
        {
        }

        /// <summary>
        /// Not-split antigens will have 0+ associated children.
        /// </summary>
        protected override IEnumerable<MatchingSerology> GetIndirectlyMatchingSerologies(SerologyFamily family)
        {
            return GetAssociatedMatchingSerologies(family.Child);
        }
    }
}
