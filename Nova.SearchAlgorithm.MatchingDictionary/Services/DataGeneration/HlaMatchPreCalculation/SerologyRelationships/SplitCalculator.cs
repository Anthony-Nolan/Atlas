using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
{
    internal class SplitCalculator: MatchingSerologyCalculatorBase
    {
        public SplitCalculator(IEnumerable<RelSerSer> serologyRelationships) :
            base(serologyRelationships)
        {
        }

        /// <summary>
        /// Split antigens will have a broad parent, and 0+ associated children.
        /// </summary>
        protected override IEnumerable<MatchingSerology> GetIndirectlyMatchingSerologies(SerologyFamily family)
        {
            var broad = GetBroadMatchingSerology(family.Parent);
            var associated = GetAssociatedMatchingSerologies(family.Child);

            return new List<MatchingSerology> { broad }.Concat(associated);
        }
    }
}
