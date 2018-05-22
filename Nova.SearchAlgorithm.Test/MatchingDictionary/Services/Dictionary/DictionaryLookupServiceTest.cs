using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
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
        private IMatchingDictionaryLookupService lookupService;
        private IMatchingDictionaryRepository repository;
        private IHlaServiceClient hlaServiceClient;
        private const MolecularLocusType MolecularLocus = MolecularLocusType.A;
        private const MatchLocus MatchedLocus = MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IMatchingDictionaryRepository>();
            hlaServiceClient = Substitute.For<IHlaServiceClient>();
            lookupService = new MatchingDictionaryLookupService(repository, hlaServiceClient);
        }

        [TestCase(HlaTypingCategory.AlleleString)]
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
        public void GetMatchingHla_WhenAllele_LookupTheTwoFieldName(string hlaName, string twoFieldName)
        {
            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);
            lookupService.GetMatchingHla(MatchedLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchedLocus, twoFieldName, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string allele = "99:01";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { allele });
            lookupService.GetMatchingHla(MatchedLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchedLocus, allele, TypingMethod.Molecular);
        }

        [TestCase("99:XX")]
        [TestCase("*99:XX")]
        public void GetMatchingHla_WhenXxCode_LookupTheFirstField(string hlaName)
        {
            const string firstField = "99";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);
            lookupService.GetMatchingHla(MatchedLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchedLocus, firstField, TypingMethod.Molecular);
        }

        [Test]
        public void GetMatchingHla_WhenSerology_LookupTheUnchangedSerology()
        {
            const string hlaName = "SEROLOGY";

            hlaServiceClient.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);
            lookupService.GetMatchingHla(MatchedLocus, hlaName);

            repository.Received().GetDictionaryEntry(MatchedLocus, hlaName, TypingMethod.Serology);
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
            repository.GetDictionaryEntry(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry, thirdEntry);
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

            repository.GetDictionaryEntry(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetDictionaryEntry(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).Returns((MatchingDictionaryEntry)null);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        private static MatchingDictionaryEntry BuildAlleleDictionaryEntry(string hlaName)
        {
            var matchingPGroups = new List<string> { hlaName };
            var matchingSerologies = new List<SerologyEntry> { new SerologyEntry("SEROLOGY", SerologySubtype.NotSplit) };

            return new MatchingDictionaryEntry(
                MatchedLocus,
                hlaName,
                TypingMethod.Molecular,
                MolecularSubtype.TwoFieldAllele,
                SerologySubtype.NotSerologyTyping,
                matchingPGroups,
                matchingSerologies);
        }
    }
}
