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

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Dictionary
{
    [TestFixture]
    public class DictionaryLookupServiceTest
    {
        private IDictionaryLookupService lookupService;
        private IMatchedHlaRepository repository;
        private IHlaServiceClient hlaServiceClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IMatchedHlaRepository>();
            hlaServiceClient = Substitute.For<IHlaServiceClient>();
            lookupService = new DictionaryLookupService(repository, hlaServiceClient);
        }

        [SetUp]
        public void EachTestSetup()
        {
        }

        [TestCase(HlaTypingCategory.AlleleString)]
        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetMatchingHla_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string matchLocus = "A";
            const string hlaName = "HLATYPING";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(matchLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string matchLocus = "A";
            const string hlaName = "XYZ:123:INVALID";
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns<Task<HlaTypingCategory>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(matchLocus, hlaName));
        }

        [TestCase("99:99", "99:99")]
        [TestCase("99:99N", "99:99N")]
        [TestCase("99:99:99", "99:99")]
        [TestCase("99:99:99L", "99:99L")]
        [TestCase("99:99:99:99", "99:99")]
        [TestCase("99:99:99:99Q", "99:99Q")]
        public void GetMatchingHla_WhenAllele_LookupTheTwoFieldName(string hlaName, string twoFieldName)
        {
            const string matchLocus = "A";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);
            lookupService.GetMatchingHla(matchLocus, hlaName);

            repository.Received().GetDictionaryEntry(matchLocus, twoFieldName, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const MolecularLocusType molecularLocus = MolecularLocusType.A;
            var matchLocus = molecularLocus.ToString();
            const string hlaName = "99:NMDPCODE";
            const string allele = "99:01";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocus, hlaName).Returns(new List<string> { allele });
            lookupService.GetMatchingHla(matchLocus, hlaName);

            repository.Received().GetDictionaryEntry(matchLocus, allele, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenXxCode_LookupTheFirstField()
        {
            const string matchLocus = "A";
            const string hlaName = "99:XX";
            const string firstField = "99";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);
            lookupService.GetMatchingHla(matchLocus, hlaName);

            repository.Received().GetDictionaryEntry(matchLocus, firstField, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenSerology_LookupTheUnchangedSerology()
        {
            const string matchLocus = "A";
            const string hlaName = "SEROLOGY";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);
            lookupService.GetMatchingHla(matchLocus, hlaName);

            repository.Received().GetDictionaryEntry(matchLocus, hlaName, TypingMethod.Serology);
        }

        [Test]
        public async Task GetMatchingHla_WhenNmdpCode_MatchingHlaForAllAllelesIsReturned()
        {
            const MolecularLocusType molecularLocus = MolecularLocusType.A;
            var matchLocus = molecularLocus.ToString();
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "99:50";
            const string thirdAllele = "99:99";
            var firstEntry = BuildAlleleDictionaryEntry(matchLocus, firstAllele);
            var secondEntry = BuildAlleleDictionaryEntry(matchLocus, secondAllele);
            var thirdEntry = BuildAlleleDictionaryEntry(matchLocus, thirdAllele);

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocus, hlaName).Returns(new List<string> { firstAllele, secondAllele, thirdAllele });
            repository.GetDictionaryEntry(matchLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry, thirdEntry);
            var actualResult = await lookupService.GetMatchingHla(matchLocus, hlaName);

            Assert.AreEqual(actualResult.MatchLocus, matchLocus);
            Assert.AreEqual(actualResult.LookupName, hlaName);
            Assert.AreEqual(actualResult.MatchingPGroups, new[] { firstAllele, secondAllele, thirdAllele });
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCodeIsInvalid_ExceptionIsThrown()
        {
            const MolecularLocusType molecularLocus = MolecularLocusType.A;
            var matchLocus = molecularLocus.ToString();
            const string hlaName = "99:INVALIDCODE";

            hlaServiceClient.GetHlaTypingCategory(hlaName)
                .Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocus, hlaName)
                .Returns<Task<List<string>>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(matchLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCodeContainsAlleleNotInRepository_ExceptionIsThrown()
        {
            const MolecularLocusType molecularLocus = MolecularLocusType.A;
            var matchLocus = molecularLocus.ToString();
            const string hlaName = "99:NMDPCODE";
            const string alleleInRepo = "99:01";
            const string alleleNotInRepo = "100:01";
            var entry = BuildAlleleDictionaryEntry(matchLocus, hlaName);

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocus, hlaName).Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            repository.GetDictionaryEntry(matchLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetDictionaryEntry(matchLocus, alleleNotInRepo, TypingMethod.Molecular).Returns((MatchingDictionaryEntry)null);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(matchLocus, hlaName));
        }

        private static MatchingDictionaryEntry BuildAlleleDictionaryEntry(string matchLocus, string hlaName)
        {
            var matchingPGroups = new List<string> { hlaName };
            var matchingSerologies = new List<SerologyEntry> { new SerologyEntry("SEROLOGY", SerologySubtype.NotSplit) };

            return new MatchingDictionaryEntry(
                matchLocus,
                hlaName,
                TypingMethod.Molecular,
                MolecularSubtype.TwoFieldAllele,
                SerologySubtype.NotSerologyType,
                matchingPGroups,
                matchingSerologies);
        }
    }
}
