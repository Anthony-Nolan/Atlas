using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on serology typings.
    /// </summary>
    internal class SerologyInfoGenerator
    {
        private class SerologyFamily
        {
            public SerologyTyping SerologyTyping { get; }
            public RelSerSer Parent { get; }
            public RelSerSer Child { get; }

            public SerologyFamily(List<RelSerSer> serologyRelationships, IWmdaHlaTyping wmdaHlaTyping, bool isDeleted = false)
            {
                Parent = GetParent(serologyRelationships, wmdaHlaTyping);
                Child = GetChild(serologyRelationships, wmdaHlaTyping);

                SerologyTyping = new SerologyTyping(
                    wmdaHlaTyping.Locus,
                    wmdaHlaTyping.Name,
                    GetSerologySubtype(wmdaHlaTyping.Name),
                    isDeleted);
            }

            public SerologyFamily(List<RelSerSer> serologyRelationships, HlaNom hlaNom) : this(serologyRelationships, hlaNom, hlaNom.IsDeleted)
            {
            }

            public static RelSerSer GetParent(IEnumerable<RelSerSer> serologyRelationships, IWmdaHlaTyping serology)
            {
                return serologyRelationships
                    .Where(relationship => relationship.LocusEquals(serology))
                    .SingleOrDefault(relationship =>
                        relationship.SplitAntigens.Contains(serology.Name) 
                        || relationship.AssociatedAntigens.Contains(serology.Name));
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

        private readonly List<HlaNom> serologies;
        private readonly List<RelSerSer> serologyRelationships;

        public SerologyInfoGenerator(IWmdaDataRepository dataRepository)
        {
            // enumerating data collections here as they will be access hundreds of times
            serologies = dataRepository.Serologies.ToList();
            serologyRelationships = dataRepository.SerologyToSerologyRelationships.ToList();
        }

        public IEnumerable<ISerologyInfoForMatching> GetSerologyInfoForMatching()
        {
            return serologies.Select(GetInfoForSingleSerology);
        }

        private ISerologyInfoForMatching GetInfoForSingleSerology(HlaNom ser)
        {
            var serFamily = new SerologyFamily(serologyRelationships, ser);
            var usedInMatching = new SerologyTyping(serFamily.SerologyTyping);
            var matchList = new List<SerologyTyping>(CalculateMatchingSerologiesFromFamily(serFamily));

            if (!ser.IdenticalHla.Equals(""))
            {
                var identicalSer = new HlaNom(TypingMethod.Serology, ser.Locus, ser.IdenticalHla);
                var identicalSerFamily = new SerologyFamily(serologyRelationships, identicalSer);
                usedInMatching = new SerologyTyping(identicalSerFamily.SerologyTyping);
                matchList.AddRange(CalculateMatchingSerologiesFromFamily(identicalSerFamily));
            }

            return new SerologyInfoForMatching(serFamily.SerologyTyping, usedInMatching, matchList);
        }

        private IEnumerable<SerologyTyping> CalculateMatchingSerologiesFromFamily(SerologyFamily family)
        {
            var serology = family.SerologyTyping;
            var parent = family.Parent;
            var child = family.Child;

            var matching = new List<SerologyTyping> { serology };

            switch (serology.SerologySubtype)
            {
                case SerologySubtype.NotSplit:
                    AddAssociated(child, matching);
                    break;

                case SerologySubtype.Split:
                    AddBroad(parent, matching);
                    AddAssociated(child, matching);
                    break;

                case SerologySubtype.Broad:
                    AddAssociated(child, matching);

                    foreach (var split in child.SplitAntigens.Select(s => new HlaNom(TypingMethod.Serology, child.Locus, s)))
                    {
                        AddSplit(split, matching);
                        AddAssociated(SerologyFamily.GetChild(serologyRelationships, split), matching);
                    }
                    break;

                case SerologySubtype.Associated:
                    var grandparent = SerologyFamily.GetParent(serologyRelationships, parent);
                    if (grandparent != null)
                    {
                        AddSplit(parent, matching);
                        AddBroad(grandparent, matching);
                    }
                    else
                        AddUnknownSubtype(parent, matching);
                    break;
            }

            return matching;
        }

        private static void AddBroad(IWmdaHlaTyping broad, List<SerologyTyping> matching)
        {
            matching.Add(new SerologyTyping(broad, SerologySubtype.Broad));
        }

        private static void AddSplit(IWmdaHlaTyping split, List<SerologyTyping> matching)
        {
            matching.Add(new SerologyTyping(split, SerologySubtype.Split));
        }

        private static void AddAssociated(RelSerSer child, List<SerologyTyping> matching)
        {
            if (child != null)
                matching.AddRange(
                    child.AssociatedAntigens.Select(a =>
                        new SerologyTyping(child.Locus, a, SerologySubtype.Associated)));
        }

        private void AddUnknownSubtype(IWmdaHlaTyping ser, List<SerologyTyping> matching)
        {
            matching.Add(new SerologyFamily(serologyRelationships, ser).SerologyTyping);
        }
    }
}
