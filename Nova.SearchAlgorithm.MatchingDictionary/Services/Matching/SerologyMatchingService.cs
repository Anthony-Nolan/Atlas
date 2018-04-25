using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    public interface ISerologyMatchingService
    {
        IEnumerable<IMatchingSerology> MatchSerologyToSerology(Func<IWmdaHlaType, bool> filter);
    }

    public class SerologyMatchingService : ISerologyMatchingService
    {
        private class SerologyFamily
        {
            public Serology Serology { get; }
            public RelSerSer Parent { get; }
            public RelSerSer Child { get; }

            public SerologyFamily(List<RelSerSer> relSerSer, IWmdaHlaType wmdaHlaType, bool isDeleted = false)
            {
                Parent = GetParent(relSerSer, wmdaHlaType);
                Child = GetChild(relSerSer, wmdaHlaType);

                Serology = new Serology(
                    wmdaHlaType.WmdaLocus,
                    wmdaHlaType.Name,
                    GetSerologySubtype(wmdaHlaType.Name),
                    isDeleted);
            }

            public SerologyFamily(List<RelSerSer> relSerSer, HlaNom hlaNom) : this(relSerSer, hlaNom, hlaNom.IsDeleted)
            {
            }

            public static RelSerSer GetParent(IEnumerable<RelSerSer> relSerSer, IWmdaHlaType serology)
            {
                return relSerSer.SingleOrDefault(r =>
                    r.WmdaLocus.Equals(serology.WmdaLocus)
                    && (r.SplitAntigens.Contains(serology.Name) || r.AssociatedAntigens.Contains(serology.Name))
                    );
            }

            public static RelSerSer GetChild(IEnumerable<RelSerSer> relSerSer, IWmdaHlaType serology)
            {
                return relSerSer.SingleOrDefault(r =>
                    r.WmdaLocus.Equals(serology.WmdaLocus) && r.Name.Equals(serology.Name));
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

        private readonly IWmdaRepository _repository;

        public SerologyMatchingService(IWmdaRepository repo)
        {
            _repository = repo;
        }

        public IEnumerable<IMatchingSerology> MatchSerologyToSerology(Func<IWmdaHlaType, bool> filter)
        {
            var relSerSer = WmdaDataFactory.GetData<RelSerSer>(_repository, filter);
            var allSerology = WmdaDataFactory.GetData<HlaNom>(_repository, filter);
            var allMatching = allSerology.Select(ser => GetSingleMatchingSerology(relSerSer, ser));

            return allMatching;
        }

        private static IMatchingSerology GetSingleMatchingSerology(IEnumerable<RelSerSer> relSerSer, HlaNom ser)
        {
            var relSerSerList = relSerSer.ToList();

            var serFamily = new SerologyFamily(relSerSerList, ser);
            var usedInMatching = new Serology(serFamily.Serology);
            var matchList = new List<Serology>(CalculateMatchingSerologiesFromFamily(relSerSerList, serFamily));

            if (!ser.IdenticalHla.Equals(""))
            {
                var identicalSer = new HlaNom(ser.WmdaLocus, ser.IdenticalHla);
                var identicalSerFamily = new SerologyFamily(relSerSerList, identicalSer);
                usedInMatching = new Serology(identicalSerFamily.Serology);
                matchList.AddRange(CalculateMatchingSerologiesFromFamily(relSerSerList, identicalSerFamily));
            }

            return new SerologyToSerology(serFamily.Serology, usedInMatching, matchList);
        }

        private static IEnumerable<Serology> CalculateMatchingSerologiesFromFamily(List<RelSerSer> relSerSer, SerologyFamily family)
        {
            var serology = family.Serology;
            var parent = family.Parent;
            var child = family.Child;

            var matching = new List<Serology> { serology };

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

                    foreach (var split in child.SplitAntigens.Select(s => new HlaNom(child.WmdaLocus, s)))
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

        private static void AddBroad(IWmdaHlaType broad, List<Serology> matching)
        {
            matching.Add(new Serology(broad, SerologySubtype.Broad));
        }

        private static void AddSplit(IWmdaHlaType split, List<Serology> matching)
        {
            matching.Add(new Serology(split, SerologySubtype.Split));
        }

        private static void AddAssociated(RelSerSer child, List<Serology> matching)
        {
            if (child != null)
                matching.AddRange(
                    child.AssociatedAntigens.Select(a =>
                        new Serology(child.WmdaLocus, a, SerologySubtype.Associated)));
        }

        private static void AddUnknownSubtype(
            List<RelSerSer> relSerSer, IWmdaHlaType ser, List<Serology> matching)
        {
            matching.Add(new SerologyFamily(relSerSer, ser).Serology);
        }
    }
}
