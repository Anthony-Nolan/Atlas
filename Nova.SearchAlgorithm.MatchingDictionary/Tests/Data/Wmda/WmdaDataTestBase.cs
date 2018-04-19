using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Tests.Repositories;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Data.Wmda
{
    public class WmdaDataTestBase<TWmdaHlaType> where TWmdaHlaType : IWmdaHlaType
    {
        protected IWmdaRepository Repo;
        protected List<TWmdaHlaType> AllHlaTypes;
        protected IEnumerable<string> MatchLoci;

        public WmdaDataTestBase(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
        {
            Repo = MockWmdaRepository.Instance;
            AllHlaTypes = WmdaDataFactory.GetData<TWmdaHlaType>(Repo, filter).ToList();
            MatchLoci = matchLoci;
        }

        [Test]
        public void WmdaCollectionOnlyContainsMatchLoci()
        {
            var collectionCopy = new List<TWmdaHlaType>(AllHlaTypes);
            Assert.IsNotEmpty(collectionCopy);

            collectionCopy.RemoveAll(m => MatchLoci.Contains(m.WmdaLocus));
            Assert.IsEmpty(collectionCopy);
        }

        protected TWmdaHlaType GetSingleWmdaHlaType(string wmdaLocus, string name)
        {
            return AllHlaTypes.Single(s => s.WmdaLocus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
