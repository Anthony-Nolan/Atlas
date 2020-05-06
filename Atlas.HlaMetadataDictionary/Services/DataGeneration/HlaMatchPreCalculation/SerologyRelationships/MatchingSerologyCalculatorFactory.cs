using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using System;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
{
    internal class MatchingSerologyCalculatorFactory
    {
        public MatchingSerologyCalculatorBase GetMatchingSerologyCalculator(
            SerologySubtype serologySubtype,
            IEnumerable<RelSerSer> serologyRelationships)
        {
            switch (serologySubtype)
            {
                case SerologySubtype.NotSplit:
                    return new NotSplitCalculator(serologyRelationships);
                case SerologySubtype.Split:
                    return new SplitCalculator(serologyRelationships);
                case SerologySubtype.Broad:
                    return new BroadCalculator(serologyRelationships);
                case SerologySubtype.Associated:
                    return new AssociatedCalculator(serologyRelationships);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
