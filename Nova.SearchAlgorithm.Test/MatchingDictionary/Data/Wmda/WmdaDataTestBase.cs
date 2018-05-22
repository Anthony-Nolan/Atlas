using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    public class WmdaDataTestBase<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected IWmdaRepository Repo;
        protected List<TWmdaHlaTyping> AllHlaTypings;
        protected IEnumerable<string> MatchLoci;

        public WmdaDataTestBase(Func<IWmdaHlaTyping, bool> filter, IEnumerable<string> matchLoci)
        {
            Repo = MockWmdaRepository.Instance;
            AllHlaTypings = WmdaDataFactory.GetData<TWmdaHlaTyping>(Repo, filter).ToList();
            MatchLoci = matchLoci;
        }

        [Test]
        public void WmdaCollectionOnlyContainsMatchLoci()
        {
            var collectionCopy = new List<TWmdaHlaTyping>(AllHlaTypings);
            Assert.IsNotEmpty(collectionCopy);

            collectionCopy.RemoveAll(m => MatchLoci.Contains(m.WmdaLocus));
            Assert.IsEmpty(collectionCopy);
        }

        protected TWmdaHlaTyping GetSingleWmdaHlaTyping(string wmdaLocus, string name)
        {
            return AllHlaTypings.Single(s => s.WmdaLocus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
