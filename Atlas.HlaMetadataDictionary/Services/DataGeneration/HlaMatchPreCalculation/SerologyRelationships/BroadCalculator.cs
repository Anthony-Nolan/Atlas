using Atlas.Common.GeneticData.Hla.Models;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships
{
    internal class BroadCalculator: MatchingSerologyCalculatorBase
    {
        public BroadCalculator(IEnumerable<RelSerSer> serologyRelationships) :
            base(serologyRelationships)
        {
        }

        /// <summary>
        /// Broad antigens will have 2+ split children, 0+ associated children, and 0+ associated grandchildren.
        /// </summary>
        protected override IEnumerable<MatchingSerology> GetIndirectlyMatchingSerologies(SerologyFamily family)
        {
            var splits = family.Child.SplitAntigens
                .Select(s => new HlaNom(TypingMethod.Serology, family.Child.TypingLocus, s))
                .ToList();

            var splitsOfBroad = splits.Select(GetSplitMatchingSerology);

            var associatedToSplits = splits.SelectMany(split =>
                GetAssociatedMatchingSerologies(SerologyFamily.GetChild(SerologyRelationships, split)));

            var associatedToBroad = GetAssociatedMatchingSerologies(family.Child);

            return splitsOfBroad
                .Concat(associatedToSplits)
                .Concat(associatedToBroad);
        }
    }
}
