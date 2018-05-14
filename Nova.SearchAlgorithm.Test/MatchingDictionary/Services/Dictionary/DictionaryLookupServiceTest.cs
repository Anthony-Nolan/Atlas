using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MatchLocus = Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes.MatchLocus;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Dictionary
{
    [TestFixture]
    public class DictionaryLookupServiceTest
    {
        private IDictionaryLookupService lookupService;
        private IMatchedHlaRepository repository;
        private IHlaServiceClient hlaServiceClient;
        private const MolecularLocusType MolecularLocus = MolecularLocusType.A;
        private const MatchLocus MatchLocus = SearchAlgorithm.MatchingDictionary.Models.HLATypes.MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IMatchedHlaRepository>();
            hlaServiceClient = Substitute.For<IHlaServiceClient>();
            lookupService = new DictionaryLookupService(repository, hlaServiceClient);
        }

        [TestCase(HlaTypingCategory.AlleleString)]
        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetMatchingHla_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns<Task<HlaTypingCategory>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchLocus, hlaName));
        }

        [TestCase("*99:99", "99:99")]
        [TestCase("99:99", "99:99")]
        [TestCase("99:99N", "99:99N")]
        [TestCase("99:99:99", "99:99")]
        [TestCase("99:99:99L", "99:99L")]
        [TestCase("99:99:99:99", "99:99")]
        [TestCase("99:99:99:99Q", "99:99Q")]
        public void GetMatchingHla_WhenAllele_LookupTheTwoFieldName(string hlaName, string twoFieldName)
        {
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);
            lookupService.GetMatchingHla(MatchLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchLocus, twoFieldName, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string allele = "99:01";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { allele });
            lookupService.GetMatchingHla(MatchLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchLocus, allele, TypingMethod.Molecular);
        }

        [TestCase("99:XX")]
        [TestCase("*99:XX")]
        public void GetMatchingHla_WhenXxCode_LookupTheFirstField(string hlaName)
        {
            const string firstField = "99";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);
            lookupService.GetMatchingHla(MatchLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchLocus, firstField, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenSerology_LookupTheUnchangedSerology()
        {
            const string hlaName = "SEROLOGY";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);
            lookupService.GetMatchingHla(MatchLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchLocus, hlaName, TypingMethod.Serology);
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
            repository.GetDictionaryEntry(MatchLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry, thirdEntry);
            var actualResult = await lookupService.GetMatchingHla(MatchLocus, hlaName);

            Assert.AreEqual(actualResult.MatchLocus, MatchLocus);
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

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchLocus, hlaName));
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

            repository.GetDictionaryEntry(MatchLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetDictionaryEntry(MatchLocus, alleleNotInRepo, TypingMethod.Molecular).Returns((MatchingDictionaryEntry)null);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchLocus, hlaName));
        }

        private static MatchingDictionaryEntry BuildAlleleDictionaryEntry(string hlaName)
        {
            var matchingPGroups = new List<string> { hlaName };
            var matchingSerologies = new List<SerologyEntry> { new SerologyEntry("SEROLOGY", SerologySubtype.NotSplit) };

            return new MatchingDictionaryEntry(
                MatchLocus,
                hlaName,
                TypingMethod.Molecular,
                MolecularSubtype.TwoFieldAllele,
                SerologySubtype.NotSerologyType,
                matchingPGroups,
                matchingSerologies);
        }
    }
}
