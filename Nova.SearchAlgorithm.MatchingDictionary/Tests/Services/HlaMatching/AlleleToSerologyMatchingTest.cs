using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.HlaMatching
{
    [TestFixtureSource(typeof(HlaMatchingTestFixtureArgs), "MatchedAlleles")]
    public class AlleleToSerologyMatchingTest : MatchedHlaTestBase<MatchedAllele>
    {
        public AlleleToSerologyMatchingTest(IEnumerable<MatchedAllele> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void ExpressedAllelesHaveCorrectMatchingSerology()
        {
            var normalAllele = GetSingleMatchingType("A", "01:01:01:01");
            var normalSerology = new List<Serology>
            {
                new Serology("A", "1", Subtype.NotSplit)
            };

            var lowAllele = GetSingleMatchingType("B", "39:01:01:02L");
            var lowSerology = new List<Serology>
            {
                new Serology("B", "3901", Subtype.Associated),
                new Serology("B", "39", Subtype.Split),
                new Serology("B", "16", Subtype.Broad)
            };

            var questionableAllele = GetSingleMatchingType("C", "07:01:01:14Q");
            var questionableSerology = new List<Serology>
            {
                new Serology("Cw", "7", Subtype.NotSplit)
            };

            var secretedAllele = GetSingleMatchingType("B", "44:02:01:02S");
            var secretedSerology = new List<Serology>
            {
                new Serology("B", "44", Subtype.Split),
                new Serology("B", "12", Subtype.Broad)
            };

            Assert.IsTrue(normalAllele.MatchingSerologies.SequenceEqual(normalSerology));
            Assert.IsTrue(lowAllele.MatchingSerologies.SequenceEqual(lowSerology));
            Assert.IsTrue(questionableAllele.MatchingSerologies.SequenceEqual(questionableSerology));
            Assert.IsTrue(secretedAllele.MatchingSerologies.SequenceEqual(secretedSerology));
        }

        [Test]
        public void NamedNonExpressedAlleleHasNoMatchingSerology()
        {
            var nullAllele = GetSingleMatchingType("A", "29:01:01:02N");
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
                s.Allele.IsNullExpressor && s.SerologyCount != 0));
        }

        [Test]
        public void IdenticalHlaUsedToFindMatchingSerologyForDeletedAlleles()
        {
            var deletedWithIdentical = GetSingleMatchingType("A", "11:53");
            var withIdenticalSer = new List<Serology>
            {
                new Serology("A", "11", Subtype.NotSplit)
            };

            var deletedIsNullIdenticalIsExpressing = GetSingleMatchingType("A", "01:34N");
            var nullToExpressingSer = new List<Serology>
            {
                new Serology("A", "1", Subtype.NotSplit)
            };

            var deletedIsExpressingIdenticalIsNull = GetSingleMatchingType("A", "03:260");

            Assert.IsTrue(deletedWithIdentical.MatchingSerologies.SequenceEqual(withIdenticalSer));
            Assert.IsTrue(deletedIsNullIdenticalIsExpressing.MatchingSerologies.SequenceEqual(nullToExpressingSer));
            Assert.IsEmpty(deletedIsExpressingIdenticalIsNull.MatchingSerologies);
        }

        [Test]
        public void DeletedAlleleWithNoIdenticalHlaHasNoMatchingSerology()
        {
            var deletedNoIdentical = GetSingleMatchingType("A", "02:100");
            Assert.IsEmpty(deletedNoIdentical.MatchingSerologies);
        }

        [Test]
        public void AlleleMappedToBroadHasCorrectMatchingSerologies()
        {
            var broadNoAssociatedAllele = GetSingleMatchingType("A", "26:10");
            var broadNoAssociatedSer = new List<Serology>
            {
                new Serology("A", "10", Subtype.Broad),
                new Serology("A", "25", Subtype.Split),
                new Serology("A", "26", Subtype.Split),
                new Serology("A", "34", Subtype.Split),
                new Serology("A", "66", Subtype.Split)
            };

            var broadWithAssociatedAllele = GetSingleMatchingType("B", "40:26");
            var broadWithAssociatedSer = new List<Serology>
            {
                new Serology("B", "21", Subtype.Broad),
                new Serology("B", "4005", Subtype.Associated),
                new Serology("B", "49", Subtype.Split),
                new Serology("B", "50", Subtype.Split),
                new Serology("B", "40", Subtype.Broad),
                new Serology("B", "60", Subtype.Split),
                new Serology("B", "61", Subtype.Split)
            };

            Assert.IsTrue(broadNoAssociatedAllele.MatchingSerologies.SequenceEqual(broadNoAssociatedSer));
            Assert.IsTrue(broadWithAssociatedAllele.MatchingSerologies.SequenceEqual(broadWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToSplitHasCorrectMatchingSerologies()
        {
            var splitNoAssociatedAllele = GetSingleMatchingType("C", "03:02:01");
            var splitNoAssociatedSer = new List<Serology>
            {
                new Serology("Cw", "10", Subtype.Split),
                new Serology("Cw", "3", Subtype.Broad)
            };

            var splitWithAssociatedAllele = GetSingleMatchingType("DRB1", "14:01:01");
            var splitWithAssociatedSer = new List<Serology>
            {
                new Serology("DR", "14", Subtype.Split),
                new Serology("DR", "6", Subtype.Broad),
                new Serology("DR", "1403", Subtype.Associated),
                new Serology("DR", "1404", Subtype.Associated)
            };

            Assert.IsTrue(splitNoAssociatedAllele.MatchingSerologies.SequenceEqual(splitNoAssociatedSer));
            Assert.IsTrue(splitWithAssociatedAllele.MatchingSerologies.SequenceEqual(splitWithAssociatedSer));
        }

        [Test]
        public void AlleleMappedToAssociatedHasCorrectMatchingSerologies()
        {
            var associatedOfBroadAllele = GetSingleMatchingType("B", "40:05:01:01");
            var associatedOfBroadSer = new List<Serology>
            {
                new Serology("B", "40", Subtype.Broad),
                new Serology("B", "60", Subtype.Split),
                new Serology("B", "61", Subtype.Split),
                new Serology("B", "4005", Subtype.Associated),
                new Serology("B", "21", Subtype.Broad)
            };

            var associatedOfSplitAllele = GetSingleMatchingType("A", "24:03:01:01");
            var associatedOfSplitSer = new List<Serology>
            {
                new Serology("A", "2403", Subtype.Associated),
                new Serology("A", "24", Subtype.Split),
                new Serology("A", "9", Subtype.Broad)
            };

            var associatedOfNotSplitAllele = GetSingleMatchingType("DRB1", "01:03:02");
            var associatedOfNotSplitSer = new List<Serology>
            {
                new Serology("DR", "103", Subtype.Associated),
                new Serology("DR", "1", Subtype.NotSplit)
            };

            Assert.IsTrue(associatedOfBroadAllele.MatchingSerologies.SequenceEqual(associatedOfBroadSer));
            Assert.IsTrue(associatedOfSplitAllele.MatchingSerologies.SequenceEqual(associatedOfSplitSer));
            Assert.IsTrue(associatedOfNotSplitAllele.MatchingSerologies.SequenceEqual(associatedOfNotSplitSer));
        }

        [Test]
        public void AlleleMappedToNotSplitHasCorrectMatchingSerologies()
        {
            var notSplitWithAssociatedAllele = GetSingleMatchingType("B", "07:02:27");
            var notSplitWithAssociatedSer = new List<Serology>
            {
                new Serology("B", "7", Subtype.NotSplit),
                new Serology("B", "703", Subtype.Associated)
            };

            var notSplitNoAssociatedAllele = GetSingleMatchingType("DQB1", "02:02:01:01");
            var notSplitNoAssociatedSer = new List<Serology>
            {
                new Serology("DQ", "2", Subtype.NotSplit)
            };

            Assert.IsTrue(notSplitWithAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitWithAssociatedSer));
            Assert.IsTrue(notSplitNoAssociatedAllele.MatchingSerologies.SequenceEqual(notSplitNoAssociatedSer));
        }

        [Test]
        public void B15AllelesHaveCorrectMatchingAndUnexpectedSerologies()
        {
            var b15BroadAllele = GetSingleMatchingType("B", "15:33");
            var b15BroadSer = new List<Serology>
            {
                new Serology("B", "15", Subtype.Broad),
                new Serology("B", "62", Subtype.Split),
                new Serology("B", "63", Subtype.Split),
                new Serology("B", "75", Subtype.Split),
                new Serology("B", "76", Subtype.Split),
                new Serology("B", "77", Subtype.Split)
            };

            var b15SplitAllele = GetSingleMatchingType("B", "15:01:01:01");
            var b15SplitSer = new List<Serology>
            {
                new Serology("B", "62", Subtype.Split),
                new Serology("B", "15", Subtype.Broad)
            };

            var b70BroadAllele = GetSingleMatchingType("B", "15:09:01");
            var b70BroadSer = new List<Serology>
            {
                new Serology("B", "70", Subtype.Broad),
                new Serology("B", "71", Subtype.Split),
                new Serology("B", "72", Subtype.Split)
            };

            var b70SplitAllele = GetSingleMatchingType("B", "15:03:01:01");
            var b70SplitSer = new List<Serology>
            {
                new Serology("B", "72", Subtype.Split),
                new Serology("B", "70", Subtype.Broad)
            };

            var b1570BroadAllele = GetSingleMatchingType("B", "15:36");
            var b1570BroadSer = new List<Serology>
            {
                new Serology("B", "15", Subtype.Broad),
                new Serology("B", "62", Subtype.Split),
                new Serology("B", "63", Subtype.Split),
                new Serology("B", "75", Subtype.Split),
                new Serology("B", "76", Subtype.Split),
                new Serology("B", "77", Subtype.Split),
                new Serology("B", "70", Subtype.Broad),
                new Serology("B", "71", Subtype.Split),
                new Serology("B", "72", Subtype.Split)
            };

            Assert.IsTrue(b15BroadAllele.MatchingSerologies.SequenceEqual(b15BroadSer));
            Assert.IsEmpty(b15BroadAllele.SerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b15SplitAllele.MatchingSerologies.SequenceEqual(b15SplitSer));
            Assert.IsEmpty(b15SplitAllele.SerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70BroadAllele.MatchingSerologies.SequenceEqual(b70BroadSer));
            Assert.IsEmpty(b70BroadAllele.SerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b70SplitAllele.MatchingSerologies.SequenceEqual(b70SplitSer));
            Assert.IsEmpty(b70SplitAllele.SerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));

            Assert.IsTrue(b1570BroadAllele.MatchingSerologies.SequenceEqual(b1570BroadSer));
            Assert.IsEmpty(b1570BroadAllele.SerologyMappings.Where(m => m.AllMatchingSerology.Any(match => match.IsUnexpected)));
        }

        [Test]
        public void UnexpectedMatchesCorrectlyCaptured()
        {
            var mappedToSameBroadAllele = GetSingleMatchingType("A", "26:10");
            var mappedToSameBroadMapping = new List<SerologyMappingInfo>
            {
                new SerologyMappingInfo(
                    new Serology("A", "10", Subtype.Broad),
                    Assignment.Unambiguous,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("A", "10", Subtype.Broad)),
                        new SerologyMatchInfo(new Serology("A", "25", Subtype.Split), true),
                        new SerologyMatchInfo(new Serology("A", "26", Subtype.Split)),
                        new SerologyMatchInfo(new Serology("A", "34", Subtype.Split), true),
                        new SerologyMatchInfo(new Serology("A", "66", Subtype.Split), true)
                    })
            };

            var mappedToDifferentBroadAllele = GetSingleMatchingType("A", "02:55");
            var mappedToDifferentBroadMapping = new List<SerologyMappingInfo>
            {
                new SerologyMappingInfo(
                    new Serology("A", "2", Subtype.NotSplit),
                    Assignment.Assumed,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("A", "2", Subtype.NotSplit)),
                        new SerologyMatchInfo(new Serology("A", "203", Subtype.Associated)),
                        new SerologyMatchInfo(new Serology("A", "210", Subtype.Associated))
                    }),
                new SerologyMappingInfo(
                    new Serology("A", "28", Subtype.Broad),
                    Assignment.Expert,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("A", "28", Subtype.Broad), true),
                        new SerologyMatchInfo(new Serology("A", "68", Subtype.Split), true),
                        new SerologyMatchInfo(new Serology("A", "69", Subtype.Split), true)
                    })
            };

            Assert.IsTrue(mappedToSameBroadAllele.SerologyMappings.SequenceEqual(mappedToSameBroadMapping));
            Assert.IsTrue(mappedToDifferentBroadAllele.SerologyMappings.SequenceEqual(mappedToDifferentBroadMapping));
        }

        [Test]
        public void AllelesWhoseFamiliesAreInvalidSerologyTypesHaveCorrectSerologyMappings()
        {
            var noAssignmentAllele = GetSingleMatchingType("C", "12:02:02:01");
            var noAssignmentMapping = new List<SerologyMappingInfo>
            {
                new SerologyMappingInfo(
                    new Serology("Cw", "12", Subtype.NotSplit),
                    Assignment.None,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("Cw", "12", Subtype.NotSplit))
                    })
            };

            var expertAssignmentAllele = GetSingleMatchingType("C", "15:07");
            var expertAssignmentMapping = new List<SerologyMappingInfo>
            {
               new SerologyMappingInfo(
                    new Serology("Cw", "3", Subtype.Broad), 
                    Assignment.Expert,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("Cw", "3", Subtype.Broad), true),
                        new SerologyMatchInfo(new Serology("Cw", "9", Subtype.Split), true),
                        new SerologyMatchInfo(new Serology("Cw", "10", Subtype.Split), true)
                    }),
                new SerologyMappingInfo(
                    new Serology("Cw", "15", Subtype.NotSplit),
                    Assignment.None,
                    new List<SerologyMatchInfo>
                    {
                        new SerologyMatchInfo(new Serology("Cw", "15", Subtype.NotSplit))
                    })
            };

            Assert.IsTrue(noAssignmentAllele.SerologyMappings.SequenceEqual(noAssignmentMapping));
            Assert.IsTrue(expertAssignmentAllele.SerologyMappings.SequenceEqual(expertAssignmentMapping));
        }
    }
}
