using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal abstract class WmdaRepositoryTestBase<TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        protected readonly string[] MolecularLoci =  { "A*", "B*", "C*", "DPB1*", "DQB1*", "DRB1*" };
        protected readonly string[] SerologyLoci = { "A", "B", "Cw", "DQ", "DR" };

        protected IWmdaDataRepository WmdaDataRepository;
        protected List<TWmdaHlaTyping> WmdaHlaTypings;

        protected abstract IEnumerable<TWmdaHlaTyping> SelectTestDataTypings(WmdaDataset dataset);
        protected abstract string[] ApplicableLoci { get; }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                WmdaDataRepository = SharedTestDataCache.GetWmdaDataRepository();
                SetupTestData();
            });
        }

        private void SetupTestData()
        {
            var wmdaDataset = WmdaDataRepository.GetWmdaDataset(SharedTestDataCache.HlaNomenclatureVersionForImportingTestWmdaRepositoryFiles);
            var data = SelectTestDataTypings(wmdaDataset);
            WmdaHlaTypings = data.ToList();
        }

        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_IsNotEmpty()
        {
            Assert.IsNotEmpty(WmdaHlaTypings);
        }

        [Test]
        public void WmdaDataRepository_WmdaHlaTypingCollection_OnlyContainsMatchLoci()
        {
            WmdaHlaTypings.Should().OnlyContain(typing => ApplicableLoci.Contains(typing.TypingLocus));
        }

        protected TWmdaHlaTyping GetSingleWmdaHlaTyping(string wmdaLocus, string name)
        {
            return WmdaHlaTypings.Single(s => s.TypingLocus.Equals(wmdaLocus) && s.Name.Equals(name));
        }
    }
}
