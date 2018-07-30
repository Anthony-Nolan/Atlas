using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships
{
    /// <summary>
    /// Calculates a serology typing's own properties, parent and child(ren)
    /// using serology relationships.
    /// </summary>
    internal class SerologyFamily
    {
        public SerologyTyping SerologyTyping { get; }
        public RelSerSer Parent { get; }
        public RelSerSer Child { get; }

        public SerologyFamily(IEnumerable<RelSerSer> serologyRelationships, IWmdaHlaTyping serology, bool isDeleted)
        {
            var relationships = serologyRelationships.ToList();
            Parent = GetParent(relationships, serology);
            Child = GetChild(relationships, serology);

            SerologyTyping = new SerologyTyping(
                serology.Locus,
                serology.Name,
                GetSerologySubtype(serology.Name),
                isDeleted);
        }

        public static RelSerSer GetParent(IEnumerable<RelSerSer> serologyRelationships, IWmdaHlaTyping serology)
        {
            return serologyRelationships
                .Where(relationship => relationship.LocusEquals(serology))
                .SingleOrDefault(relationship =>
                    relationship.SplitAntigens.Contains(serology.Name) ||
                    relationship.AssociatedAntigens.Contains(serology.Name));
        }

        public static RelSerSer GetChild(IEnumerable<RelSerSer> serologyRelationships, IWmdaHlaTyping serology)
        {
            return serologyRelationships
                .SingleOrDefault(relationship => relationship.TypingEquals(serology));
        }

        private SerologySubtype GetSerologySubtype(string serologyName)
        {
            if (Parent != null)
            {
                if (Parent.SplitAntigens.Contains(serologyName))
                    return SerologySubtype.Split;
                if (Parent.AssociatedAntigens.Contains(serologyName))
                    return SerologySubtype.Associated;
            }

            if (Child != null && Child.SplitAntigens.Any())
                return SerologySubtype.Broad;

            return SerologySubtype.NotSplit;
        }
    }
}
