using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
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
