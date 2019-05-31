using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
{
    internal class AssociatedCalculator: MatchingSerologyCalculatorBase
    {
        public AssociatedCalculator(IEnumerable<RelSerSer> serologyRelationships) :
            base(serologyRelationships)
        {
        }

        /// <summary>
        /// Associated antigens can be directly mapped to a broad, or split, or not-split.
        /// </summary>
        protected override IEnumerable<MatchingSerology> GetIndirectlyMatchingSerologies(SerologyFamily family)
        {
            var grandparentOfAssociated = SerologyFamily.GetParent(SerologyRelationships, family.Parent);

            if (grandparentOfAssociated == null)
            {
                // the parent of an associated antigen that has no 'grandparent'
                // could either be a broad or not-split antigen
                var broadOrNotSplit = GetMatchingSerologyOfUnknownSubtype(family.Parent);
                return new[] { broadOrNotSplit };
            }

            // if there is a grandparent: parent = split, grandparent = broad.
            var split = GetSplitMatchingSerology(family.Parent);
            var broad = GetBroadMatchingSerology(grandparentOfAssociated);

            return new List<MatchingSerology> { split, broad };
        }
    }
}
