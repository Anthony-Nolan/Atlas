using Atlas.Common.Public.Models.GeneticData;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.HaplotypeFrequencySet;
using Atlas.ManualTesting.Test.TestHelpers;
using Atlas.MatchPrediction.Models.FileSchema;
using FluentAssertions;
using LochNessBuilder;
using NUnit.Framework;

namespace Atlas.ManualTesting.Test.Tests
{
    /// <summary>
    /// Assertions within fixture should align with the data found in <see cref="TestHaplotypeFrequencySet.TransformerTestSet"/>,
    /// else tests will fail.
    /// </summary>
    [TestFixture]
    internal class HaplotypeFrequencySetTransformerTests
    {
        private IHaplotypeFrequencySetTransformer transformer;
        private TestHaplotypeFrequencySet testSet;

        private const Locus TestLocus = Locus.A;
        private const string Target = "43:02N";
        private const string Replacement = "43:01";
        private readonly Builder<FindReplaceHlaNames> hlaNamesBuilder = Builder<FindReplaceHlaNames>.New
            .With(x => x.Locus, TestLocus)
            .With(x => x.TargetHlaName, Target)
            .With(x => x.ReplacementHlaName, Replacement);

        [SetUp]
        public void SetUp()
        {
            testSet = new TestHaplotypeFrequencySet();
            transformer = new HaplotypeFrequencySetTransformer();
        }

        [Test]
        public void TransformHaplotypeFrequencySet_NoFrequenciesInSet_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => transformer.TransformHaplotypeFrequencySet(testSet.EmptySetWithNoFrequencies, default));
        }

        [Test]
        public void TransformHaplotypeFrequencySet_ReplacesTargetHlaName()
        {
            TransformedFrequencyRecords()
                .Select(f => f.A)
                .Distinct()
                .Should().NotContain(Target);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_MergesDuplicateHaplotypes()
        {
            // - original record count in test file is 6
            // - one haplotype contains the target HLA name
            // - when that record is updated with the replacement value it should lead to 2 duplicate haplotypes
            // - the transformer should merge the duplicates, leaving a count of 5
            TransformedFrequencyRecords().Count.Should().Be(5);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_TransformedRecordHasExpectedHlaNamesAndFrequency()
        {
            // - one haplotype in test file contains the target HLA name
            // - when that is updated with the replacement value it should lead to 2 duplicate haplotypes
            // - the transformer should merge the duplicates, and apply the frequency sum to the transformed record
            var record = TransformedFrequencyRecords().Single(f => f.A == Replacement);
            record.B.Should().Be("15:10g");
            record.C.Should().Be("04:01g");
            record.Dqb1.Should().Be("03:02g");
            record.Drb1.Should().Be("04:01g");
            record.Frequency.Should().Be(7.613e-7m);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_RecordDoesNotHaveTargetHlaName_DoesNotAlterRecord()
        {
            var record = TransformedFrequencyRecords().Single(f => f.A == "01:01g");
            record.B.Should().Be("08:01g");
            record.C.Should().Be("07:01g");
            record.Dqb1.Should().Be("02:01g");
            record.Drb1.Should().Be("03:01g");
            record.Frequency.Should().Be(0.0579917457m);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_PreservesMetadata()
        {
            var set = TransformedSet().Set;

            set.HlaNomenclatureVersion.Should().Be("3480");
            set.RegistryCodes.Should().BeEquivalentTo("123", "456", "789");
            set.EthnicityCodes.Should().BeEquivalentTo(null, "AF", "UK");
            set.PopulationId.Should().Be(999);
            set.TypingCategory.Should().Be(ImportTypingCategory.SmallGGroup);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_ReturnsOriginalRecordsContainingTarget()
        {
            // one haplotype in test file contains the target HLA name
            var record = TransformedSet().OriginalRecordsContainingTarget.Single();

            record.A.Should().Be("43:02N");
            record.B.Should().Be("15:10g");
            record.C.Should().Be("04:01g");
            record.Dqb1.Should().Be("03:02g");
            record.Drb1.Should().Be("04:01g");
            record.Frequency.Should().Be(9.52e-8m);
        }

        [Test]
        public void TransformHaplotypeFrequencySet_ReturnsOriginalRecordCount()
        {
            TransformedSet().OriginalRecordCount.Should().Be(6);
        }

        private ICollection<FrequencyRecord> TransformedFrequencyRecords() => TransformedSet().Set.Frequencies.ToList();

        private TransformedHaplotypeFrequencySet TransformedSet() => transformer
            .TransformHaplotypeFrequencySet(testSet.TransformerTestSet, hlaNamesBuilder);
    }
}