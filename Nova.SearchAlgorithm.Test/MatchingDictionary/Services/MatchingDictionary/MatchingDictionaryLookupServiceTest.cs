using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class MatchingDictionaryLookupServiceTest
    {
        private IMatchingDictionaryLookupService lookupService;
        private IMatchingDictionaryRepository repository;
        private IHlaServiceClient hlaServiceClient;
        private const MolecularLocusType MolecularLocus = MolecularLocusType.A;
        private const MatchLocus MatchedLocus = MatchLocus.A;
        private const string TypingLocus = "A";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IMatchingDictionaryRepository>();
            hlaServiceClient = Substitute.For<IHlaServiceClient>();
            lookupService = new MatchingDictionaryLookupService(repository, hlaServiceClient);
        }

        [SetUp]
        public void SetupBeforeEachTest()
        {
            repository.ClearReceivedCalls();

            var fakeResultToPreventInvalidHlaExceptionBeingRaised = BuildAlleleDictionaryEntry("hlaName");
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), Arg.Any<TypingMethod>())
                .Returns(fakeResultToPreventInvalidHlaExceptionBeingRaised);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetMatchingHla_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns<Task<HlaTypingCategory>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [TestCase("*99:99", "99:99")]
        [TestCase("99:99", "99:99")]
        [TestCase("99:99N", "99:99N")]
        [TestCase("99:99:99", "99:99")]
        [TestCase("99:99:99L", "99:99L")]
        [TestCase("99:99:99:99", "99:99")]
        [TestCase("99:99:99:99Q", "99:99Q")]
        public async Task GetMatchingHla_WhenAllele_LookupTheTwoFieldName(string hlaName, string twoFieldName)
        {
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);
            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, twoFieldName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetMatchingHla_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "100:01";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { firstAllele, secondAllele });
            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received(2).GetMatchingDictionaryEntryIfExists(
                MatchedLocus, Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), TypingMethod.Molecular);
        }

        [TestCase("99:01/02", "99:01", "99:02")]
        [TestCase("99:10/11N", "99:10", "99:11N")]
        [TestCase("99:22/100:01:01:01", "99:22", "100:01:01:01")]
        [TestCase("99:33L/100:33", "99:33L", "100:33")]
        public async Task GetMatchingHla_WhenAlleleString_LookupTheAlleleList(string hlaName, string firstAllele,
            string secondAllele)
        {
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.AlleleString);
            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received(2).GetMatchingDictionaryEntryIfExists(
                MatchedLocus, Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), TypingMethod.Molecular);
        }

        [TestCase("99:XX")]
        [TestCase("*99:XX")]
        public async Task GetMatchingHla_WhenXxCode_LookupTheFirstField(string hlaName)
        {
            const string firstField = "99";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);
            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, firstField, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetMatchingHla_WhenSerology_LookupTheUnchangedSerology()
        {
            const string hlaName = "SEROLOGY";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);
            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, hlaName, TypingMethod.Serology);
        }

        [Test]
        public async Task GetMatchingHla_WhenNmdpCode_MatchingHlaForAllAllelesIsReturned()
        {
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "99:50";
            const string thirdAllele = "99:99";
            var firstEntry = BuildAlleleDictionaryEntry(firstAllele);
            var secondEntry = BuildAlleleDictionaryEntry(secondAllele);
            var thirdEntry = BuildAlleleDictionaryEntry(thirdAllele);

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { firstAllele, secondAllele, thirdAllele });
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry, thirdEntry);
            var actualResult = await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            Assert.AreEqual(actualResult.MatchLocus, MatchedLocus);
            Assert.AreEqual(actualResult.LookupName, hlaName);
            Assert.AreEqual(actualResult.MatchingPGroups, new[] { firstAllele, secondAllele, thirdAllele });
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCodeIsInvalid_ExceptionIsThrown()
        {
            const string hlaName = "99:INVALIDCODE";

            hlaServiceClient.GetHlaTypingCategory(hlaName)
                .Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName)
                .Returns<Task<List<string>>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCodeContainsAlleleNotInRepository_ExceptionIsThrown()
        {
            const string hlaName = "99:NMDPCODE";
            const string alleleInRepo = "99:01";
            const string alleleNotInRepo = "100:01";
            var entry = BuildAlleleDictionaryEntry(hlaName);

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).Returns((MatchingDictionaryEntry)null);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [TestCase("99:01/02", "99:01", "99:02")]
        [TestCase("99:10/11N", "99:10", "99:11N")]
        [TestCase("99:22/100:01:01:01", "99:22", "100:01:01:01")]
        [TestCase("99:33L/100:33", "99:33L", "100:33")]
        public async Task GetMatchingHla_WhenAlleleString_MatchingHlaForAllAllelesIsReturned(string hlaName, string firstAllele, string secondAllele)
        {
            var firstEntry = BuildAlleleDictionaryEntry(firstAllele);
            var secondEntry = BuildAlleleDictionaryEntry(secondAllele);

            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry);
            var actualResult = await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            Assert.AreEqual(actualResult.MatchLocus, MatchedLocus);
            Assert.AreEqual(actualResult.LookupName, hlaName);
            Assert.AreEqual(actualResult.MatchingPGroups, new[] { firstAllele, secondAllele });
        }

        [TestCase("99:01/02", "99:01", "99:02")]
        [TestCase("99:10/11N", "99:10", "99:11N")]
        [TestCase("99:22/100:01:01:01", "99:22", "100:01:01:01")]
        [TestCase("99:33L/100:33", "99:33L", "100:33")]
        public void GetMatchingHla_WhenAlleleStringContainsAlleleNotInRepository_ExceptionIsThrown(string hlaName, string alleleInRepo, string alleleNotInRepo)
        {
            var entry = BuildAlleleDictionaryEntry(hlaName);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).Returns((MatchingDictionaryEntry)null);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }
        
        private static MatchingDictionaryEntry BuildAlleleDictionaryEntry(string hlaName)
        {
            var matchedAllele = Substitute.For<IMatchingDictionarySource<AlleleTyping>>();
            matchedAllele.TypingForMatchingDictionary.Returns(new AlleleTyping(TypingLocus + "*", hlaName));
            matchedAllele.MatchingPGroups.Returns(new List<string> { hlaName });
            matchedAllele.MatchingGGroups.Returns(new List<string> { hlaName });
            matchedAllele.MatchingSerologies.Returns(new List<SerologyTyping> { new SerologyTyping(TypingLocus, "SEROLOGY", SerologySubtype.NotSplit) });

            return new MatchingDictionaryEntry(matchedAllele, hlaName, MolecularSubtype.TwoFieldAllele);
        }
    }
}
