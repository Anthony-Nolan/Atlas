using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedAlleles))]
    public class AlleleToSerologyMatchingTest : MatchedOnTestBase<MatchedAllele>
    {
        public AlleleToSerologyMatchingTest(IEnumerable<MatchedAllele> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void ExpressedAllelesHaveCorrectMatchingSerology()
        {
            var normalAllele = GetSingleMatchingTyping(MatchLocus.A, "01:01:01:01");
            var normalSerology = new List<SerologyTyping>
            {
                new SerologyTyping("A", "1", SerologySubtype.NotSplit)
            };

            var lowAllele = GetSingleMatchingTyping(MatchLocus.B, "39:01:01:02L");
            var lowSerology = new List<SerologyTyping>
            {
                new SerologyTyping("B", "3901", SerologySubtype.Associated),
                new SerologyTyping("B", "39", SerologySubtype.Split),
                new SerologyTyping("B", "16", SerologySubtype.Broad)
            };

            var questionableAllele = GetSingleMatchingTyping(MatchLocus.C, "07:01:01:14Q");
            var questionableSerology = new List<SerologyTyping>
            {
                new SerologyTyping("Cw", "7", SerologySubtype.NotSplit)
            };

            var secretedAllele = GetSingleMatchingTyping(MatchLocus.B, "44:02:01:02S");
            var secretedSerology = new List<SerologyTyping>
            {
                new SerologyTyping("B", "44", SerologySubtype.Split),
                new SerologyTyping("B", "12", SerologySubtype.Broad)
            };

            Assert.IsTrue(normalAllele.MatchingSerologies.SequenceEqual(normalSerology));
            Assert.IsTrue(lowAllele.MatchingSerologies.SequenceEqual(lowSerology));
            Assert.IsTrue(questionableAllele.MatchingSerologies.SequenceEqual(questionableSerology));
            Assert.IsTrue(secretedAllele.MatchingSerologies.SequenceEqual(secretedSerology));
        }

        [Test]
        public void NamedNonExpressedAlleleHasNoMatchingSerology()
        {
            var nullAllele = GetSingleMatchingTyping(MatchLocus.A, "29:01:01:02N");
            Assert.IsEmpty(nullAllele.MatchingSerologies);
        }

        [Test]
        public void AllNonExpressedAllelesHaveNoMatchingSerology()
        {
            var serologyCounts = MatchingTypings
                .Where(m => !m.HlaTyping.IsDeleted && m.HlaTyping is AlleleTyping)
                .Select(m => new
                {
                    Allele = m.HlaTyping as AlleleTyping,
                    SerologyCount = m.MatchingSerologies.Count()
                });

            Assert.IsEmpty(serologyCounts.Where(s =>
                s.Allele.IsNullExpresser && s.SerologyCount != 0));
        }

        [Test]
        public void IdenticalHlaUsedToFindMatchingSerologyForDeletedAlleles()
        {
            var deletedWithIdentical = GetSingleMatchingTyping(MatchLocus.A, "11:53");
            var withIdenticalSer = new List<SerologyTyping>
            {
                new SerologyTyping("A", "11", SerologySubtype.NotSplit)
            };

            var deletedIsNullIdenticalIsExpressing = GetSingleMatchingTyping(MatchLocus.A, "01:34N");
            var nullToExpressingSer = new List<SerologyTyping>
            {
                new SerologyTyping("A", "1", SerologySubtype.NotSplit)
            };

            var deletedIsExpressingIdenticalIsNull = GetSingleMatchingTyping(MatchLocus.A, "03:260");

            Assert.IsTrue(deletedWithIdentical.MatchingSerologies.SequenceEqual(withIdenticalSer));
            Assert.IsTrue(deletedIsNullIdenticalIsExpressing.MatchingSerologies.SequenceEqual(nullToExpressingSer));
            Assert.IsEmpty(deletedIsExpressingIdenticalIsNull.MatchingSerologies);
        }

        [Test]
        public void DeletedAlleleWithNoIdenticalHlaHasNoMatchingSerology()
        {
            var deletedNoIdentical = GetSingleMatchingTyping(MatchLocus.A, "02:100");
            Assert.IsEmpty(deletedNoIdentical.MatchingSerologies);
        }

        [Test]
        public void AlleleMappedToBroadHasCorrectMatchingSerologies()
        {
            var broadNoAssociatedAllele = GetSingleMatchingTyping(MatchLocus.A, "26:10");
            var broadNoAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("A", "10", SerologySubtype.Broad),
                new SerologyTyping("A", "25", SerologySubtype.Split),
                new SerologyTyping("A", "26", SerologySubtype.Split),
                new SerologyTyping("A", "34", SerologySubtype.Split),
                new SerologyTyping("A", "66", SerologySubtype.Split)
            };

            var broadWithAssociatedAllele = GetSingleMatchingTyping(MatchLocus.B, "40:26");
            var broadWithAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "21", SerologySubtype.Broad),
                new SerologyTyping("B", "4005", SerologySubtype.Associated),
                new SerologyTyping("B", "49", SerologySubtype.Split),
                new SerologyTyping("B", "50", SerologySubtype.Split),
                new SerologyTyping("B", "40", SerologySubtype.Broad),
                new SerologyTyping("B", "60", SerologySubtype.Split),
                new SerologyTyping("B", "61", SerologySubtype.Split)
            };

            Assert.IsTrue(broadNoAssociatedAllele.MatchingSerologies.SequenceEqual(broadNoAssociatedSer));
            Assert.IsTrue(broadWithAssociatedAllele.MatchingSerologies.SequenceEqual(broadWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToSplitHasCorrectMatchingSerologies()
        {
            var splitNoAssociatedAllele = GetSingleMatchingTyping(MatchLocus.C, "03:02:01");
            var splitNoAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("Cw", "10", SerologySubtype.Split),
                new SerologyTyping("Cw", "3", SerologySubtype.Broad)
            };

            var splitWithAssociatedAllele = GetSingleMatchingTyping(MatchLocus.Drb1, "14:01:01");
            var splitWithAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("DR", "14", SerologySubtype.Split),
                new SerologyTyping("DR", "6", SerologySubtype.Broad),
                new SerologyTyping("DR", "1403", SerologySubtype.Associated),
                new SerologyTyping("DR", "1404", SerologySubtype.Associated)
            };

            Assert.IsTrue(splitNoAssociatedAllele.MatchingSerologies.SequenceEqual(splitNoAssociatedSer));
            Assert.IsTrue(splitWithAssociatedAllele.MatchingSerologies.SequenceEqual(splitWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToAssociatedHasCorrectMatchingSerologies()
        {
            var associatedOfBroadAllele = GetSingleMatchingTyping(MatchLocus.B, "40:05:01:01");
            var associatedOfBroadSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "40", SerologySubtype.Broad),
                new SerologyTyping("B", "60", SerologySubtype.Split),
                new SerologyTyping("B", "61", SerologySubtype.Split),
                new SerologyTyping("B", "4005", SerologySubtype.Associated),
                new SerologyTyping("B", "21", SerologySubtype.Broad)
            };

            var associatedOfSplitAllele = GetSingleMatchingTyping(MatchLocus.A, "24:03:01:01");
            var associatedOfSplitSer = new List<SerologyTyping>
            {
                new SerologyTyping("A", "2403", SerologySubtype.Associated),
                new SerologyTyping("A", "24", SerologySubtype.Split),
                new SerologyTyping("A", "9", SerologySubtype.Broad)
            };

            var associatedOfNotSplitAllele = GetSingleMatchingTyping(MatchLocus.Drb1, "01:03:02");
            var associatedOfNotSplitSer = new List<SerologyTyping>
            {
                new SerologyTyping("DR", "103", SerologySubtype.Associated),
                new SerologyTyping("DR", "1", SerologySubtype.NotSplit)
            };

            Assert.IsTrue(associatedOfBroadAllele.MatchingSerologies.SequenceEqual(associatedOfBroadSer));
            Assert.IsTrue(associatedOfSplitAllele.MatchingSerologies.SequenceEqual(associatedOfSplitSer));
            Assert.IsTrue(associatedOfNotSplitAllele.MatchingSerologies.SequenceEqual(associatedOfNotSplitSer));
        }

        [Test]
        public void AlleleMappedToNotSplitHasCorrectMatchingSerologies()
        {
            var notSplitWithAssociatedAllele = GetSingleMatchingTyping(MatchLocus.B, "07:02:27");
            var notSplitWithAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "7", SerologySubtype.NotSplit),
                new SerologyTyping("B", "703", SerologySubtype.Associated)
            };

            var notSplitNoAssociatedAllele = GetSingleMatchingTyping(MatchLocus.Dqb1, "02:02:01:01");
            var notSplitNoAssociatedSer = new List<SerologyTyping>
            {
                new SerologyTyping("DQ", "2", SerologySubtype.NotSplit)
            };

            Assert.IsTrue(notSplitWithAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitWithAssociatedSer));
            Assert.IsTrue(notSplitNoAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitNoAssociatedSer));
        }

        [Test]
        public void B15AllelesHaveCorrectMatchingAndUnexpectedSerologies()
        {
            var b15BroadAllele = GetSingleMatchingTyping(MatchLocus.B, "15:33");
            var b15BroadSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "15", SerologySubtype.Broad),
                new SerologyTyping("B", "62", SerologySubtype.Split),
                new SerologyTyping("B", "63", SerologySubtype.Split),
                new SerologyTyping("B", "75", SerologySubtype.Split),
                new SerologyTyping("B", "76", SerologySubtype.Split),
                new SerologyTyping("B", "77", SerologySubtype.Split)
            };

            var b15SplitAllele = GetSingleMatchingTyping(MatchLocus.B, "15:01:01:01");
            var b15SplitSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "62", SerologySubtype.Split),
                new SerologyTyping("B", "15", SerologySubtype.Broad)
            };

            var b70BroadAllele = GetSingleMatchingTyping(MatchLocus.B, "15:09:01");
            var b70BroadSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "70", SerologySubtype.Broad),
                new SerologyTyping("B", "71", SerologySubtype.Split),
                new SerologyTyping("B", "72", SerologySubtype.Split)
            };

            var b70SplitAllele = GetSingleMatchingTyping(MatchLocus.B, "15:03:01:01");
            var b70SplitSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "72", SerologySubtype.Split),
                new SerologyTyping("B", "70", SerologySubtype.Broad)
            };

            var b1570BroadAllele = GetSingleMatchingTyping(MatchLocus.B, "15:36");
            var b1570BroadSer = new List<SerologyTyping>
            {
                new SerologyTyping("B", "15", SerologySubtype.Broad),
                new SerologyTyping("B", "62", SerologySubtype.Split),
                new SerologyTyping("B", "63", SerologySubtype.Split),
                new SerologyTyping("B", "75", SerologySubtype.Split),
                new SerologyTyping("B", "76", SerologySubtype.Split),
                new SerologyTyping("B", "77", SerologySubtype.Split),
                new SerologyTyping("B", "70", SerologySubtype.Broad),
                new SerologyTyping("B", "71", SerologySubtype.Split),
                new SerologyTyping("B", "72", SerologySubtype.Split)
            };

            Assert.IsTrue(b15BroadAllele.MatchingSerologies.SequenceEqual(b15BroadSer));
            Assert.IsEmpty(b15BroadAllele.DnaToSerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b15SplitAllele.MatchingSerologies.SequenceEqual(b15SplitSer));
            Assert.IsEmpty(b15SplitAllele.DnaToSerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70BroadAllele.MatchingSerologies.SequenceEqual(b70BroadSer));
            Assert.IsEmpty(b70BroadAllele.DnaToSerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70SplitAllele.MatchingSerologies.SequenceEqual(b70SplitSer));
            Assert.IsEmpty(b70SplitAllele.DnaToSerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b1570BroadAllele.MatchingSerologies.SequenceEqual(b1570BroadSer));
            Assert.IsEmpty(b1570BroadAllele.DnaToSerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));
        }

        [Test]
        public void UnexpectedMatchesCorrectlyCaptured()
        {
            var mappedToSameBroadAllele = GetSingleMatchingTyping(MatchLocus.A, "26:10");
            var mappedToSameBroadMapping = new List<DnaToSerologyMapping>
            {
                new DnaToSerologyMapping(
                    new SerologyTyping("A", "10", SerologySubtype.Broad),
                    Assignment.Unambiguous,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("A", "10", SerologySubtype.Broad)),
                        new DnaToSerologyMatch(new SerologyTyping("A", "25", SerologySubtype.Split), true),
                        new DnaToSerologyMatch(new SerologyTyping("A", "26", SerologySubtype.Split)),
                        new DnaToSerologyMatch(new SerologyTyping("A", "34", SerologySubtype.Split), true),
                        new DnaToSerologyMatch(new SerologyTyping("A", "66", SerologySubtype.Split), true)
                    })
            };

            var mappedToDifferentBroadAllele = GetSingleMatchingTyping(MatchLocus.A, "02:55");
            var mappedToDifferentBroadMapping = new List<DnaToSerologyMapping>
            {
                new DnaToSerologyMapping(
                    new SerologyTyping("A", "2", SerologySubtype.NotSplit),
                    Assignment.Assumed,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("A", "2", SerologySubtype.NotSplit)),
                        new DnaToSerologyMatch(new SerologyTyping("A", "203", SerologySubtype.Associated)),
                        new DnaToSerologyMatch(new SerologyTyping("A", "210", SerologySubtype.Associated))
                    }),
                new DnaToSerologyMapping(
                    new SerologyTyping("A", "28", SerologySubtype.Broad),
                    Assignment.Expert,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("A", "28", SerologySubtype.Broad), true),
                        new DnaToSerologyMatch(new SerologyTyping("A", "68", SerologySubtype.Split), true),
                        new DnaToSerologyMatch(new SerologyTyping("A", "69", SerologySubtype.Split), true)
                    })
            };

            Assert.IsTrue(mappedToSameBroadAllele.DnaToSerologyMappings.SequenceEqual(mappedToSameBroadMapping));
            Assert.IsTrue(mappedToDifferentBroadAllele.DnaToSerologyMappings.SequenceEqual(mappedToDifferentBroadMapping));
        }

        [Test]
        public void AllelesWhoseFamiliesAreInvalidSerologyTypingsHaveCorrectSerologyMappings()
        {
            var noAssignmentAllele = GetSingleMatchingTyping(MatchLocus.C, "12:02:02:01");
            var noAssignmentMapping = new List<DnaToSerologyMapping>
            {
                new DnaToSerologyMapping(
                    new SerologyTyping("Cw", "12", SerologySubtype.NotSerologyTyping),
                    Assignment.None,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("Cw", "12", SerologySubtype.NotSerologyTyping))
                    })
            };

            var expertAssignmentAllele = GetSingleMatchingTyping(MatchLocus.C, "15:07");
            var expertAssignmentMapping = new List<DnaToSerologyMapping>
            {
               new DnaToSerologyMapping(
                    new SerologyTyping("Cw", "3", SerologySubtype.Broad), 
                    Assignment.Expert,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("Cw", "3", SerologySubtype.Broad), true),
                        new DnaToSerologyMatch(new SerologyTyping("Cw", "9", SerologySubtype.Split), true),
                        new DnaToSerologyMatch(new SerologyTyping("Cw", "10", SerologySubtype.Split), true)
                    }),
                new DnaToSerologyMapping(
                    new SerologyTyping("Cw", "15", SerologySubtype.NotSerologyTyping),
                    Assignment.None,
                    new List<DnaToSerologyMatch>
                    {
                        new DnaToSerologyMatch(new SerologyTyping("Cw", "15", SerologySubtype.NotSerologyTyping))
                    })
            };

            Assert.IsTrue(noAssignmentAllele.DnaToSerologyMappings.SequenceEqual(noAssignmentMapping));
            Assert.IsTrue(expertAssignmentAllele.DnaToSerologyMappings.SequenceEqual(expertAssignmentMapping));
        }
    }
}
