using System;
using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships
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
