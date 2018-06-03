using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
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

            public SerologyFamily(List<RelSerSer> relSerSer, IWmdaHlaTyping wmdaHlaTyping, bool isDeleted = false)
            {
                Parent = GetParent(relSerSer, wmdaHlaTyping);
                Child = GetChild(relSerSer, wmdaHlaTyping);

                SerologyTyping = new SerologyTyping(
                    wmdaHlaTyping.Locus,
                    wmdaHlaTyping.Name,
                    GetSerologySubtype(wmdaHlaTyping.Name),
                    isDeleted);
            }

            public SerologyFamily(List<RelSerSer> relSerSer, HlaNom hlaNom) : this(relSerSer, hlaNom, hlaNom.IsDeleted)
            {
            }

            public static RelSerSer GetParent(IEnumerable<RelSerSer> relSerSer, IWmdaHlaTyping serology)
            {
                return relSerSer.SingleOrDefault(r =>
                    r.Locus.Equals(serology.Locus)
                    && (r.SplitAntigens.Contains(serology.Name) || r.AssociatedAntigens.Contains(serology.Name))
                    );
            }

            public static RelSerSer GetChild(IEnumerable<RelSerSer> relSerSer, IWmdaHlaTyping serology)
            {
                return relSerSer.SingleOrDefault(r =>
                    r.Locus.Equals(serology.Locus) && r.Name.Equals(serology.Name));
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

        public IEnumerable<ISerologyInfoForMatching> GetSerologyInfoForMatching(IWmdaDataRepository dataRepository)
        {
            var serologyRelationshipsList = dataRepository.SerologyToSerologyRelationships.ToList();

            var serologyInfo = dataRepository.Serologies
                .Select(serology => GetInfoForSingleSerology(serologyRelationshipsList, serology));

            return serologyInfo;
        }

        private static ISerologyInfoForMatching GetInfoForSingleSerology(List<RelSerSer> serologyRelationships, HlaNom ser)
        {
            var serFamily = new SerologyFamily(serologyRelationships, ser);
            var usedInMatching = new SerologyTyping(serFamily.SerologyTyping);
            var matchList = new List<SerologyTyping>(CalculateMatchingSerologiesFromFamily(serologyRelationships, serFamily));

            if (!ser.IdenticalHla.Equals(""))
            {
                var identicalSer = new HlaNom(TypingMethod.Serology, ser.Locus, ser.IdenticalHla);
                var identicalSerFamily = new SerologyFamily(serologyRelationships, identicalSer);
                usedInMatching = new SerologyTyping(identicalSerFamily.SerologyTyping);
                matchList.AddRange(CalculateMatchingSerologiesFromFamily(serologyRelationships, identicalSerFamily));
            }

            return new SerologyInfoForMatching(serFamily.SerologyTyping, usedInMatching, matchList);
        }

        private static IEnumerable<SerologyTyping> CalculateMatchingSerologiesFromFamily(List<RelSerSer> relSerSer, SerologyFamily family)
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
                        AddAssociated(SerologyFamily.GetChild(relSerSer, split), matching);
                    }
                    break;

                case SerologySubtype.Associated:
                    var grandparent = SerologyFamily.GetParent(relSerSer, parent);
                    if (grandparent != null)
                    {
                        AddSplit(parent, matching);
                        AddBroad(grandparent, matching);
                    }
                    else
                        AddUnknownSubtype(relSerSer, parent, matching);
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

        private static void AddUnknownSubtype(
            List<RelSerSer> relSerSer, IWmdaHlaTyping ser, List<SerologyTyping> matching)
        {
            matching.Add(new SerologyFamily(relSerSer, ser).SerologyTyping);
        }
    }
}
