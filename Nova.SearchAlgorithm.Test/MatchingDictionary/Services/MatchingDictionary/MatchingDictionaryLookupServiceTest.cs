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
        private IAlleleNamesLookupService alleleNamesLookupService;
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
            alleleNamesLookupService = Substitute.For<IAlleleNamesLookupService>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
            memoryCache = Substitute.For<IMemoryCache>();
            logger = Substitute.For<ILogger>();

            lookupService = new MatchingDictionaryLookupService(
                repository,
                alleleNamesLookupService,
                hlaServiceClient,
                hlaCategorisationService,
                alleleStringSplitterService,
                memoryCache,
                logger);
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

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [Test]
        public void GetMatchingHla_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Throws(new Exception());

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
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
            alleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> { firstAllele, secondAllele });

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

        [Test]
        public async Task GetMatchingHla_WhenSubmittedAlleleNameNotFound_LookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

            // return null on submitted name to emulate scenario that requires a two-field-name lookup
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular)
                .ReturnsNull();
            // return fake entry for current name to prevent invalid hla exception
            var entryFromCurrentName = BuildAlleleDictionaryEntry(currentAlleleName);
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular)
                .Returns(entryFromCurrentName);

            await lookupService.GetMatchingHla(MatchedLocus, submittedAlleleName);

            await repository.Received().GetMatchingDictionaryEntryIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetMatchingHla_WhenSubmittedAlleleNameIsFound_DoNotLookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

            var entryBasedOnLookupName = BuildAlleleDictionaryEntry(submittedAlleleName);
            repository
                .GetMatchingDictionaryEntryIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular)
                .Returns(entryBasedOnLookupName);

            await lookupService.GetMatchingHla(MatchedLocus, submittedAlleleName);

            await repository.DidNotReceive().GetMatchingDictionaryEntryIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular);
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
            const string expectedLookupName = "99:NMDPCODE";

            const string firstAlleleName = "99:01";
            const string secondAlleleName = "99:50";
            const string thirdAlleleName = "99:99";
            var expectedMatchingPGroups = new[] { firstAlleleName, secondAlleleName, thirdAlleleName };

            var firstEntry = BuildAlleleDictionaryEntry(firstAlleleName);
            var secondEntry = BuildAlleleDictionaryEntry(secondAlleleName);
            var thirdEntry = BuildAlleleDictionaryEntry(thirdAlleleName);
            
            hlaCategorisationService.GetHlaTypingCategory(expectedLookupName).Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, expectedLookupName).Returns(new List<string> { firstAlleleName, secondAlleleName, thirdAlleleName });
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry, thirdEntry);

            var actualResult = await lookupService.GetMatchingHla(MatchedLocus, expectedLookupName);

            Assert.AreEqual(MatchedLocus, actualResult.MatchLocus);
            Assert.AreEqual(expectedLookupName, actualResult.LookupName);
            Assert.AreEqual(expectedMatchingPGroups, actualResult.MatchingPGroups);
        }

        [Test]
        public void GetMatchingHla_WhenNmdpCodeIsInvalid_ExceptionIsThrown()
        {
            const string hlaName = "99:INVALIDCODE";

            hlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(HlaTypingCategory.NmdpCode);
            hlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName)
                .Returns<Task<List<string>>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
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

            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleInRepo }));
            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleNotInRepo }));

            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetMatchingHla_WhenAlleleString_MatchingHlaForAllAllelesIsReturned(
            HlaTypingCategory category, string expectedLookupName, string firstAlleleName, string secondAlleleName)
        {
            var expectedMatchingPGroups = new[] { firstAlleleName, secondAlleleName };
            var firstEntry = BuildAlleleDictionaryEntry(firstAlleleName);
            var secondEntry = BuildAlleleDictionaryEntry(secondAlleleName);

            hlaCategorisationService.GetHlaTypingCategory(expectedLookupName).Returns(category);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular).Returns(firstEntry, secondEntry);

            var actualResult = await lookupService.GetMatchingHla(MatchedLocus, expectedLookupName);

            Assert.AreEqual(MatchedLocus, actualResult.MatchLocus);
            Assert.AreEqual(expectedLookupName, actualResult.LookupName);
            Assert.AreEqual(expectedMatchingPGroups , actualResult.MatchingPGroups);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public void GetMatchingHla_WhenAlleleStringContainsAlleleNotInRepository_ExceptionIsThrown(
            HlaTypingCategory category, string hlaName, string alleleInRepo, string alleleNotInRepo)
        {
            var entry = BuildAlleleDictionaryEntry(hlaName);

            hlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(category);

            alleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName)
                .Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleInRepo }));
            alleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleNotInRepo }));

            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular)
                .Returns(entry);
            repository.GetMatchingDictionaryEntryIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular)
                .ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(async () => await lookupService.GetMatchingHla(MatchedLocus, hlaName));
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
