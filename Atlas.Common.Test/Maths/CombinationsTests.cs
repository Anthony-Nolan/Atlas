using System;
using System.Linq;
using Atlas.Common.Maths;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Maths
{
    [TestFixture]
    public class CombinationsTests
    {
        [TestCase(0, false, 0)]
        [TestCase(1, false, 0)]
        [TestCase(2, false, 1)]
        [TestCase(100, false, 4950)]
        [TestCase(0, true, 0)]
        [TestCase(1, true, 1)]
        [TestCase(2, true, 3)]
        [TestCase(100, true, 5050)]
        public void NumberOfPairs_CalculatesCorrectValue(int input, bool includeSelfPairs, int expectedPairCount)
        {
            var numberOfPairs = Combinations.NumberOfPairs(input, includeSelfPairs);

            numberOfPairs.Should().Be(expectedPairCount);
        }

        [Test]
        public void NumberOfPairs_WithNegativeLength_ThrowsException()
        {
            Func<long> action = () => Combinations.NumberOfPairs(-1);

            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void AllPairs_IncludingSelfPairs_IncludesSelfPairs()
        {
            var array = new[] {1, 2};

            var expectedPairs = new[]
            {
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(2, 2)
            }.AsEnumerable();

            var actualPairs = Combinations.AllPairs(array, true);

            actualPairs.Should().BeEquivalentTo(expectedPairs);
        }

        [Test]
        public void AllPairs_NotIncludingSelfPairs_DoesNotIncludesSelfPairs()
        {
            var array = new[] {1, 2, 3};

            var actualPairs = Combinations.AllPairs(array).ToList();

            actualPairs.Count.Should().Be(3);
            actualPairs.Should().NotContain(new Tuple<int, int>(1, 1));
            actualPairs.Should().NotContain(new Tuple<int, int>(2, 2));
            actualPairs.Should().NotContain(new Tuple<int, int>(3, 3));
        }

        [Test]
        public void AllPairs_WithZeroElements_ReturnsEmptyCollection()
        {
            var array = new int[] { };

            var actualPairs = Combinations.AllPairs(array);
            var actualPairsIncludingSelf = Combinations.AllPairs(array, true);

            actualPairs.Count().Should().Be(0);
            actualPairsIncludingSelf.Count().Should().Be(0);
        }

        [Test]
        public void AllPairs_WithSingleElement_IncludingSelfPairs_ReturnsSinglePair()
        {
            var array = new[] {1};

            var actualPairs = Combinations.AllPairs(array, true);

            actualPairs.Single().Should().BeEquivalentTo(new Tuple<int, int>(1, 1));
        }

        [Test]
        public void AllPairs_WithSingleElement_NotIncludingSelfPairs_ReturnsEmptyCollection()
        {
            var array = new[] {1};

            var actualPairs = Combinations.AllPairs(array);

            actualPairs.Count().Should().Be(0);
        }

        [Test]
        public void AllPairs_WithDuplicateElements_IncludingSelfPairs_ReturnsDuplicatesIncludingSelfPairs()
        {
            var array = new[] {1, 1, 2};

            var actualPairs = Combinations.AllPairs(array, true);

            actualPairs.Should().BeEquivalentTo(
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(2, 2)
            );
        }
        
        [Test]
        public void AllPairs_WithDuplicateElements_NotIncludingSelfPairs_ReturnsDuplicatesNotIncludingSelfPairs()
        {
            var array = new[] {1, 1, 2};

            var actualPairs = Combinations.AllPairs(array);

            actualPairs.Should().BeEquivalentTo(
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(1, 2)
            );
        }
    }
}