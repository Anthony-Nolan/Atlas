﻿using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships
{
    internal abstract class MatchingSerologyCalculatorBase
    {
        protected readonly List<RelSerSer> SerologyRelationships;

        protected MatchingSerologyCalculatorBase(IEnumerable<RelSerSer> serologyRelationships)
        {
            SerologyRelationships = serologyRelationships.ToList();
        }

        public IEnumerable<MatchingSerology> GetMatchingSerologies(SerologyFamily family)
        {
            var indirectMatches = GetIndirectlyMatchingSerologies(family);
            var directMatch = GetDirectlyMatchedSerology(family);

            return indirectMatches.Concat(new[] {directMatch});
        }

        protected abstract IEnumerable<MatchingSerology> GetIndirectlyMatchingSerologies(SerologyFamily family);

        protected static IEnumerable<MatchingSerology> GetAssociatedMatchingSerologies(RelSerSer child)
        {
            if (child == null)
            {
                return new List<MatchingSerology>();
            }

            return child.AssociatedAntigens
                .Select(a => new SerologyTyping(child.TypingLocus, a, SerologySubtype.Associated))
                .Select(ser => new MatchingSerology(ser, false));
        }

        protected static MatchingSerology GetSplitMatchingSerology(IWmdaHlaTyping split)
        {
            var splitSerology = new SerologyTyping(split, SerologySubtype.Split);
            return new MatchingSerology(splitSerology, false);
        }

        protected static MatchingSerology GetBroadMatchingSerology(IWmdaHlaTyping broad)
        {
            var broadSerology = new SerologyTyping(broad, SerologySubtype.Broad);
            return new MatchingSerology(broadSerology, false);
        }

        protected MatchingSerology GetMatchingSerologyOfUnknownSubtype(IWmdaHlaTyping ser)
        {
            var unknownSerology = new SerologyFamily(SerologyRelationships, ser, false).SerologyTyping;
            return new MatchingSerology(unknownSerology, false);
        }

        private static MatchingSerology GetDirectlyMatchedSerology(SerologyFamily family)
        {
            return new MatchingSerology(family.SerologyTyping, true);
        }
    }
}
