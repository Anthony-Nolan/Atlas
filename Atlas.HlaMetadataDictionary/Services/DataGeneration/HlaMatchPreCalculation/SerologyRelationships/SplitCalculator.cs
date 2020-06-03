using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships
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
