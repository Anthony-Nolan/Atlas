using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
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
