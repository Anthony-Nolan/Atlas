using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class MatchingDictionaryLookupServiceTest
    {
        private IMatchingDictionaryLookupService lookupService;
        private IMatchingDictionaryRepository repository;
        private IHlaServiceClient hlaServiceClient;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleStringSplitterService alleleStringSplitterService;
        private IMemoryCache memoryCache;
        private ILogger logger;
        private const MolecularLocusType MolecularLocus = MolecularLocusType.A;
        private const MatchLocus MatchedLocus = MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IMatchingDictionaryRepository>();
            hlaServiceClient = Substitute.For<IHlaServiceClient>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
            memoryCache = Substitute.For<IMemoryCache>();
            logger = Substitute.For<ILogger>();
            
            lookupService = new MatchingDictionaryLookupService(repository, hlaServiceClient, hlaCategorisationService, alleleStringSplitterService, memoryCache, logger);
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
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Throws(new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [Test]
        public async Task GetMatchingHla_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "100:01";

            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { firstAllele, secondAllele });

            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received(2).GetMatchingDictionaryEntryIfExists(
                MatchedLocus, Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), TypingMethod.Molecular);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetMatchingHla_WhenAlleleString_LookupTheAlleleList(
            HlaTypingCategory typingCategory, string hlaName, string firstAllele, string secondAllele)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            hlaServiceClient.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> { firstAllele, secondAllele });

            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received(2).GetMatchingDictionaryEntryIfExists(
                MatchedLocus, Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), TypingMethod.Molecular);
        }

        [TestCase("99:XX")]
        [TestCase("*99:XX")]
        public async Task GetMatchingHla_WhenXxCode_LookupTheFirstField(string hlaName)
        {
            const string firstField = "99";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);

            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, firstField, TypingMethod.Molecular);
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetMatchingHla_WhenAllele_LookupTheSubmittedHlaName(
            string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, lookupName, TypingMethod.Molecular);
        }

        [TestCase("99:99:99", "99:99")]
        [TestCase("99:99:99L", "99:99L")]
        [TestCase("99:99:99:99", "99:99")]
        [TestCase("99:99:99:99Q", "99:99Q")]
        public async Task GetMatchingHla_WhenSubmittedAlleleNameNotFound_LookupTheTwoFieldNameVariant(
            string submittedAlleleName, string expectedTwoFieldName)
        {
            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            // return null on submitted name to emulate scenario that requires a two-field-name lookup
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular)
                .ReturnsNull();
            // return fake entry on two field name to prevent invalid hla exception
            var entryFromTwoFieldName = BuildAlleleDictionaryEntry(expectedTwoFieldName);
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, expectedTwoFieldName, TypingMethod.Molecular)
                .Returns(entryFromTwoFieldName);

            await lookupService.GetMatchingHla(MatchedLocus, submittedAlleleName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, expectedTwoFieldName, TypingMethod.Molecular);
        }

        [TestCase("99:99:99", "99:99")]
        [TestCase("99:99:99L", "99:99L")]
        [TestCase("99:99:99:99", "99:99")]
        [TestCase("99:99:99:99Q", "99:99Q")]
        public async Task GetMatchingHla_WhenSubmittedAlleleNameIsFound_DoNotLookupTheTwoFieldNameVariant(
            string submittedLookupName, string expectedTwoFieldName)
        {
            hlaCategorisationService.GetHlaTypingCategory(submittedLookupName)
                .Returns(HlaTypingCategory.Allele);

            var entryBasedOnLookupName = BuildAlleleDictionaryEntry(submittedLookupName);
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, submittedLookupName, TypingMethod.Molecular)
                .Returns(entryBasedOnLookupName);

            await lookupService.GetMatchingHla(MatchedLocus, submittedLookupName);

            await repository.DidNotReceive().GetMatchingDictionaryEntryIfExists(MatchedLocus, expectedTwoFieldName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetMatchingHla_WhenSerology_LookupTheSubmittedHlaName()
        {
            const string hlaName = "SerologyName";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);

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

            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
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

            hlaCategorisationService.GetHlaTypingCategory(hlaName)
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

            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetMatchingHla_WhenAlleleString_MatchingHlaForAllAllelesIsReturned(
            HlaTypingCategory category, string hlaName, string firstAllele, string secondAllele)
        {
            var firstEntry = BuildAlleleDictionaryEntry(firstAllele);
            var secondEntry = BuildAlleleDictionaryEntry(secondAllele);

            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry);

            var actualResult = await lookupService.GetMatchingHla(MatchedLocus, hlaName);

            Assert.AreEqual(actualResult.MatchLocus, MatchedLocus);
            Assert.AreEqual(actualResult.LookupName, hlaName);
            Assert.AreEqual(actualResult.MatchingPGroups, new[] { firstAllele, secondAllele });
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public void GetMatchingHla_WhenAlleleStringContainsAlleleNotInRepository_ExceptionIsThrown(
            HlaTypingCategory category, string hlaName, string alleleInRepo, string alleleNotInRepo)
        {
            var entry = BuildAlleleDictionaryEntry(hlaName);

            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }
        
        private static MatchingDictionaryEntry BuildAlleleDictionaryEntry(string hlaName)
        {
            var matchedAllele = Substitute.For<IMatchingDictionarySource<AlleleTyping>>();
            matchedAllele.TypingForMatchingDictionary.Returns(new AlleleTyping(MatchedLocus, hlaName));
            matchedAllele.MatchingPGroups.Returns(new List<string> { hlaName });
            matchedAllele.MatchingGGroups.Returns(new List<string> { hlaName });
            matchedAllele.MatchingSerologies.Returns(new List<SerologyTyping> { new SerologyTyping(MatchedLocus.ToString(), "SEROLOGY", SerologySubtype.NotSplit) });

            return new MatchingDictionaryEntry(matchedAllele, hlaName, MolecularSubtype.TwoFieldAllele);
        }
    }
}
