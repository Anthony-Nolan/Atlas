using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class WmdaRepositoryTestBase<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected List<TWmdaHlaTyping> WmdaHlaTypings;
        protected IEnumerable<string> MatchLoci;

        public WmdaRepositoryTestBase(IEnumerable<TWmdaHlaTyping> hlaTypings, IEnumerable<string> matchLoci)
        {
            WmdaHlaTypings = hlaTypings.ToList();
            MatchLoci = matchLoci;
        }

        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_IsNotEmpty()
        {
            Assert.IsNotEmpty(WmdaHlaTypings);
        }

        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_OnlyContainsMatchLoci()
        {
            WmdaHlaTypings.Should().OnlyContain(typing => MatchLoci.Contains(typing.Locus));               
        }

        protected TWmdaHlaTyping GetSingleWmdaHlaTyping(string wmdaLocus, string name)
        {
            return WmdaHlaTypings.Single(s => s.Locus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
