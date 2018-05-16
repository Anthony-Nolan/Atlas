using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.HlaMatching
{
    [TestFixtureSource(typeof(HlaMatchingTestFixtureArgs), "MatchedAlleles")]
    public class AlleleToSerologyMatchingTest : MatchedOnTestBase<MatchedAllele>
    {
        public AlleleToSerologyMatchingTest(IEnumerable<MatchedAllele> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void ExpressedAllelesHaveCorrectMatchingSerology()
        {
            var normalAllele = GetSingleMatchingType(MatchLocus.A, "01:01:01:01");
            var normalSerology = new List<Serology>
            {
                new Serology("A", "1", SerologySubtype.NotSplit)
            };

            var lowAllele = GetSingleMatchingType(MatchLocus.B, "39:01:01:02L");
            var lowSerology = new List<Serology>
            {
                new Serology("B", "3901", SerologySubtype.Associated),
                new Serology("B", "39", SerologySubtype.Split),
                new Serology("B", "16", SerologySubtype.Broad)
            };

            var questionableAllele = GetSingleMatchingType(MatchLocus.C, "07:01:01:14Q");
            var questionableSerology = new List<Serology>
            {
                new Serology("Cw", "7", SerologySubtype.NotSplit)
            };

            var secretedAllele = GetSingleMatchingType(MatchLocus.B, "44:02:01:02S");
            var secretedSerology = new List<Serology>
            {
                new Serology("B", "44", SerologySubtype.Split),
                new Serology("B", "12", SerologySubtype.Broad)
            };

            Assert.IsTrue(normalAllele.MatchingSerologies.SequenceEqual(normalSerology));
            Assert.IsTrue(lowAllele.MatchingSerologies.SequenceEqual(lowSerology));
            Assert.IsTrue(questionableAllele.MatchingSerologies.SequenceEqual(questionableSerology));
            Assert.IsTrue(secretedAllele.MatchingSerologies.SequenceEqual(secretedSerology));
        }

        [Test]
        public void NamedNonExpressedAlleleHasNoMatchingSerology()
        {
            var nullAllele = GetSingleMatchingType(MatchLocus.A, "29:01:01:02N");
            Assert.IsEmpty(nullAllele.MatchingSerologies);
        }

        [Test]
        public void AllNonExpressedAllelesHaveNoMatchingSerology()
        {
            var serologyCounts = MatchingTypes
                .Where(m => !m.HlaType.IsDeleted && m.HlaType is Allele)
                .Select(m => new
                {
                    Allele = m.HlaType as Allele,
                    SerologyCount = m.MatchingSerologies.Count()
                });

            Assert.IsEmpty(serologyCounts.Where(s =>
                s.Allele.IsNullExpresser && s.SerologyCount != 0));
        }

        [Test]
        public void IdenticalHlaUsedToFindMatchingSerologyForDeletedAlleles()
        {
            var deletedWithIdentical = GetSingleMatchingType(MatchLocus.A, "11:53");
            var withIdenticalSer = new List<Serology>
            {
                new Serology("A", "11", SerologySubtype.NotSplit)
            };

            var deletedIsNullIdenticalIsExpressing = GetSingleMatchingType(MatchLocus.A, "01:34N");
            var nullToExpressingSer = new List<Serology>
            {
                new Serology("A", "1", SerologySubtype.NotSplit)
            };

            var deletedIsExpressingIdenticalIsNull = GetSingleMatchingType(MatchLocus.A, "03:260");

            Assert.IsTrue(deletedWithIdentical.MatchingSerologies.SequenceEqual(withIdenticalSer));
            Assert.IsTrue(deletedIsNullIdenticalIsExpressing.MatchingSerologies.SequenceEqual(nullToExpressingSer));
            Assert.IsEmpty(deletedIsExpressingIdenticalIsNull.MatchingSerologies);
        }

        [Test]
        public void DeletedAlleleWithNoIdenticalHlaHasNoMatchingSerology()
        {
            var deletedNoIdentical = GetSingleMatchingType(MatchLocus.A, "02:100");
            Assert.IsEmpty(deletedNoIdentical.MatchingSerologies);
        }

        [Test]
        public void AlleleMappedToBroadHasCorrectMatchingSerologies()
        {
            var broadNoAssociatedAllele = GetSingleMatchingType(MatchLocus.A, "26:10");
            var broadNoAssociatedSer = new List<Serology>
            {
                new Serology("A", "10", SerologySubtype.Broad),
                new Serology("A", "25", SerologySubtype.Split),
                new Serology("A", "26", SerologySubtype.Split),
                new Serology("A", "34", SerologySubtype.Split),
                new Serology("A", "66", SerologySubtype.Split)
            };

            var broadWithAssociatedAllele = GetSingleMatchingType(MatchLocus.B, "40:26");
            var broadWithAssociatedSer = new List<Serology>
            {
                new Serology("B", "21", SerologySubtype.Broad),
                new Serology("B", "4005", SerologySubtype.Associated),
                new Serology("B", "49", SerologySubtype.Split),
                new Serology("B", "50", SerologySubtype.Split),
                new Serology("B", "40", SerologySubtype.Broad),
                new Serology("B", "60", SerologySubtype.Split),
                new Serology("B", "61", SerologySubtype.Split)
            };

            Assert.IsTrue(broadNoAssociatedAllele.MatchingSerologies.SequenceEqual(broadNoAssociatedSer));
            Assert.IsTrue(broadWithAssociatedAllele.MatchingSerologies.SequenceEqual(broadWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToSplitHasCorrectMatchingSerologies()
        {
            var splitNoAssociatedAllele = GetSingleMatchingType(MatchLocus.C, "03:02:01");
            var splitNoAssociatedSer = new List<Serology>
            {
                new Serology("Cw", "10", SerologySubtype.Split),
                new Serology("Cw", "3", SerologySubtype.Broad)
            };

            var splitWithAssociatedAllele = GetSingleMatchingType(MatchLocus.Drb1, "14:01:01");
            var splitWithAssociatedSer = new List<Serology>
            {
                new Serology("DR", "14", SerologySubtype.Split),
                new Serology("DR", "6", SerologySubtype.Broad),
                new Serology("DR", "1403", SerologySubtype.Associated),
                new Serology("DR", "1404", SerologySubtype.Associated)
            };

            Assert.IsTrue(splitNoAssociatedAllele.MatchingSerologies.SequenceEqual(splitNoAssociatedSer));
            Assert.IsTrue(splitWithAssociatedAllele.MatchingSerologies.SequenceEqual(splitWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToAssociatedHasCorrectMatchingSerologies()
        {
            var associatedOfBroadAllele = GetSingleMatchingType(MatchLocus.B, "40:05:01:01");
            var associatedOfBroadSer = new List<Serology>
            {
                new Serology("B", "40", SerologySubtype.Broad),
                new Serology("B", "60", SerologySubtype.Split),
                new Serology("B", "61", SerologySubtype.Split),
                new Serology("B", "4005", SerologySubtype.Associated),
                new Serology("B", "21", SerologySubtype.Broad)
            };

            var associatedOfSplitAllele = GetSingleMatchingType(MatchLocus.A, "24:03:01:01");
            var associatedOfSplitSer = new List<Serology>
            {
                new Serology("A", "2403", SerologySubtype.Associated),
                new Serology("A", "24", SerologySubtype.Split),
                new Serology("A", "9", SerologySubtype.Broad)
            };

            var associatedOfNotSplitAllele = GetSingleMatchingType(MatchLocus.Drb1, "01:03:02");
            var associatedOfNotSplitSer = new List<Serology>
            {
                new Serology("DR", "103", SerologySubtype.Associated),
                new Serology("DR", "1", SerologySubtype.NotSplit)
            };

            Assert.IsTrue(associatedOfBroadAllele.MatchingSerologies.SequenceEqual(associatedOfBroadSer));
            Assert.IsTrue(associatedOfSplitAllele.MatchingSerologies.SequenceEqual(associatedOfSplitSer));
            Assert.IsTrue(associatedOfNotSplitAllele.MatchingSerologies.SequenceEqual(associatedOfNotSplitSer));
        }

        [Test]
        public void AlleleMappedToNotSplitHasCorrectMatchingSerologies()
        {
            var notSplitWithAssociatedAllele = GetSingleMatchingType(MatchLocus.B, "07:02:27");
            var notSplitWithAssociatedSer = new List<Serology>
            {
                new Serology("B", "7", SerologySubtype.NotSplit),
                new Serology("B", "703", SerologySubtype.Associated)
            };

            var notSplitNoAssociatedAllele = GetSingleMatchingType(MatchLocus.Dqb1, "02:02:01:01");
            var notSplitNoAssociatedSer = new List<Serology>
            {
                new Serology("DQ", "2", SerologySubtype.NotSplit)
            };

            Assert.IsTrue(notSplitWithAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitWithAssociatedSer));
            Assert.IsTrue(notSplitNoAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitNoAssociatedSer));
        }

        [Test]
        public void B15AllelesHaveCorrectMatchingAndUnexpectedSerologies()
        {
            var b15BroadAllele = GetSingleMatchingType(MatchLocus.B, "15:33");
            var b15BroadSer = new List<Serology>
            {
                new Serology("B", "15", SerologySubtype.Broad),
                new Serology("B", "62", SerologySubtype.Split),
                new Serology("B", "63", SerologySubtype.Split),
                new Serology("B", "75", SerologySubtype.Split),
                new Serology("B", "76", SerologySubtype.Split),
                new Serology("B", "77", SerologySubtype.Split)
            };

            var b15SplitAllele = GetSingleMatchingType(MatchLocus.B, "15:01:01:01");
            var b15SplitSer = new List<Serology>
            {
                new Serology("B", "62", SerologySubtype.Split),
                new Serology("B", "15", SerologySubtype.Broad)
            };

            var b70BroadAllele = GetSingleMatchingType(MatchLocus.B, "15:09:01");
            var b70BroadSer = new List<Serology>
            {
                new Serology("B", "70", SerologySubtype.Broad),
                new Serology("B", "71", SerologySubtype.Split),
                new Serology("B", "72", SerologySubtype.Split)
            };

            var b70SplitAllele = GetSingleMatchingType(MatchLocus.B, "15:03:01:01");
            var b70SplitSer = new List<Serology>
            {
                new Serology("B", "72", SerologySubtype.Split),
                new Serology("B", "70", SerologySubtype.Broad)
            };

            var b1570BroadAllele = GetSingleMatchingType(MatchLocus.B, "15:36");
            var b1570BroadSer = new List<Serology>
            {
                new Serology("B", "15", SerologySubtype.Broad),
                new Serology("B", "62", SerologySubtype.Split),
                new Serology("B", "63", SerologySubtype.Split),
                new Serology("B", "75", SerologySubtype.Split),
                new Serology("B", "76", SerologySubtype.Split),
                new Serology("B", "77", SerologySubtype.Split),
                new Serology("B", "70", SerologySubtype.Broad),
                new Serology("B", "71", SerologySubtype.Split),
                new Serology("B", "72", SerologySubtype.Split)
            };

            Assert.IsTrue(b15BroadAllele.MatchingSerologies.SequenceEqual(b15BroadSer));
            Assert.IsEmpty(b15BroadAllele.RelDnaSerMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b15SplitAllele.MatchingSerologies.SequenceEqual(b15SplitSer));
            Assert.IsEmpty(b15SplitAllele.RelDnaSerMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70BroadAllele.MatchingSerologies.SequenceEqual(b70BroadSer));
            Assert.IsEmpty(b70BroadAllele.RelDnaSerMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70SplitAllele.MatchingSerologies.SequenceEqual(b70SplitSer));
            Assert.IsEmpty(b70SplitAllele.RelDnaSerMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b1570BroadAllele.MatchingSerologies.SequenceEqual(b1570BroadSer));
            Assert.IsEmpty(b1570BroadAllele.RelDnaSerMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));
        }

        [Test]
        public void UnexpectedMatchesCorrectlyCaptured()
        {
            var mappedToSameBroadAllele = GetSingleMatchingType(MatchLocus.A, "26:10");
            var mappedToSameBroadMapping = new List<RelDnaSerMapping>
            {
                new RelDnaSerMapping(
                    new Serology("A", "10", SerologySubtype.Broad),
                    Assignment.Unambiguous,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("A", "10", SerologySubtype.Broad)),
                        new RelDnaSerMatch(new Serology("A", "25", SerologySubtype.Split), true),
                        new RelDnaSerMatch(new Serology("A", "26", SerologySubtype.Split)),
                        new RelDnaSerMatch(new Serology("A", "34", SerologySubtype.Split), true),
                        new RelDnaSerMatch(new Serology("A", "66", SerologySubtype.Split), true)
                    })
            };

            var mappedToDifferentBroadAllele = GetSingleMatchingType(MatchLocus.A, "02:55");
            var mappedToDifferentBroadMapping = new List<RelDnaSerMapping>
            {
                new RelDnaSerMapping(
                    new Serology("A", "2", SerologySubtype.NotSplit),
                    Assignment.Assumed,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("A", "2", SerologySubtype.NotSplit)),
                        new RelDnaSerMatch(new Serology("A", "203", SerologySubtype.Associated)),
                        new RelDnaSerMatch(new Serology("A", "210", SerologySubtype.Associated))
                    }),
                new RelDnaSerMapping(
                    new Serology("A", "28", SerologySubtype.Broad),
                    Assignment.Expert,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("A", "28", SerologySubtype.Broad), true),
                        new RelDnaSerMatch(new Serology("A", "68", SerologySubtype.Split), true),
                        new RelDnaSerMatch(new Serology("A", "69", SerologySubtype.Split), true)
                    })
            };

            Assert.IsTrue(mappedToSameBroadAllele.RelDnaSerMappings.SequenceEqual(mappedToSameBroadMapping));
            Assert.IsTrue(mappedToDifferentBroadAllele.RelDnaSerMappings.SequenceEqual(mappedToDifferentBroadMapping));
        }

        [Test]
        public void AllelesWhoseFamiliesAreInvalidSerologyTypesHaveCorrectSerologyMappings()
        {
            var noAssignmentAllele = GetSingleMatchingType(MatchLocus.C, "12:02:02:01");
            var noAssignmentMapping = new List<RelDnaSerMapping>
            {
                new RelDnaSerMapping(
                    new Serology("Cw", "12", SerologySubtype.NotSplit),
                    Assignment.None,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("Cw", "12", SerologySubtype.NotSplit))
                    })
            };

            var expertAssignmentAllele = GetSingleMatchingType(MatchLocus.C, "15:07");
            var expertAssignmentMapping = new List<RelDnaSerMapping>
            {
               new RelDnaSerMapping(
                    new Serology("Cw", "3", SerologySubtype.Broad), 
                    Assignment.Expert,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("Cw", "3", SerologySubtype.Broad), true),
                        new RelDnaSerMatch(new Serology("Cw", "9", SerologySubtype.Split), true),
                        new RelDnaSerMatch(new Serology("Cw", "10", SerologySubtype.Split), true)
                    }),
                new RelDnaSerMapping(
                    new Serology("Cw", "15", SerologySubtype.NotSplit),
                    Assignment.None,
                    new List<RelDnaSerMatch>
                    {
                        new RelDnaSerMatch(new Serology("Cw", "15", SerologySubtype.NotSplit))
                    })
            };

            Assert.IsTrue(noAssignmentAllele.RelDnaSerMappings.SequenceEqual(noAssignmentMapping));
            Assert.IsTrue(expertAssignmentAllele.RelDnaSerMappings.SequenceEqual(expertAssignmentMapping));
        }
    }
}
