using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class WmdaRepositoryTestBase<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected List<TWmdaHlaTyping> HlaTypings;
        protected IEnumerable<string> MatchLoci;

        public WmdaRepositoryTestBase(IEnumerable<TWmdaHlaTyping> hlaTypings, IEnumerable<string> matchLoci)
        {
            HlaTypings = hlaTypings.ToList();
            MatchLoci = matchLoci;
        }

        [Test]
        public void WmdaHlaTypingCollectionOnlyContainsMatchLoci()
        {
            var collectionCopy = new List<TWmdaHlaTyping>(HlaTypings);
            Assert.IsNotEmpty(collectionCopy);

            collectionCopy.RemoveAll(m => MatchLoci.Contains(m.Locus));
            Assert.IsEmpty(collectionCopy);
        }

        protected TWmdaHlaTyping GetSingleWmdaHlaTyping(string wmdaLocus, string name)
        {
            return HlaTypings.Single(s => s.Locus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
