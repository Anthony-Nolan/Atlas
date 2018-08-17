using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public abstract class WmdaRepositoryTestBase<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected readonly string[] MolecularLoci =  { "A*", "B*", "C*", "DPB1*", "DQB1*", "DRB1*" };

        // TODO: NOVA-1298 - Confirm that no DPB1 serology should be imported into matching dictionary
        protected readonly string[] SerologyLoci = { "A", "B", "Cw", "DQ", "DR" };

        protected IWmdaDataRepository WmdaDataRepository;
        
        protected List<TWmdaHlaTyping> WmdaHlaTypings;
        private IEnumerable<string> matchLoci;

        protected void SetTestData(IEnumerable<TWmdaHlaTyping> hlaTypings, IEnumerable<string> matchLoci)
        {
            WmdaHlaTypings = hlaTypings.ToList();
            this.matchLoci = matchLoci;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            WmdaDataRepository = SharedTestDataCache.GetWmdaDataRepository();
            SetupTestData();
        }

        protected abstract void SetupTestData();
        
        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_IsNotEmpty()
        {
            Assert.IsNotEmpty(WmdaHlaTypings);
        }

        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_OnlyContainsMatchLoci()
        {
            WmdaHlaTypings.Should().OnlyContain(typing => matchLoci.Contains(typing.Locus));               
        }

        protected TWmdaHlaTyping GetSingleWmdaHlaTyping(string wmdaLocus, string name)
        {
            return WmdaHlaTypings.Single(s => s.Locus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
