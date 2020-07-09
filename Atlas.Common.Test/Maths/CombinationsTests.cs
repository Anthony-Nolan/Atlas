using System;
using System.Linq;
using Atlas.Common.Maths;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Maths
{
    [TestFixture]
    // TODO: ATLAS-400: More tests if we end up using this in MPA
    public class CombinationsTests
    {
        [Test]
        public void AllPairs_IncludingSelfPairs_IncludesDuplicates()
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
        public void AllPairs_WithZeroElements_ReturnsEmptyCollection()
        {
            var array = new int[] {};

            var actualPairs = Combinations.AllPairs(array);
            var actualPairsIncludingSelf = Combinations.AllPairs(array, true);

            actualPairs.Count().Should().Be(0);
            actualPairsIncludingSelf.Count().Should().Be(0);
        }
        
        [Test]
        public void AllPairs_WithSingleElement_NotIncludingSelfPairs_ReturnsEmptyCollection()
        {
            var array = new[] {1};

            var actualPairs = Combinations.AllPairs(array);

            actualPairs.Count().Should().Be(0);
        }
        
        [Test]
        public void AllPairs_WithSingleElement_IncludingSelfPairs_ReturnsSinglePair()
        {
            var array = new[] {1};

            var actualPairs = Combinations.AllPairs(array, true);

            actualPairs.Single().Should().BeEquivalentTo(new Tuple<int, int>(1, 1));
        }
        
        [Test]
        public void AllPairs_NotIncludingSelfPairs_DoesNotIncludesDuplicates()
        {
            var array = new[] {1, 2, 3};

            var actualPairs = Combinations.AllPairs(array).ToList();

            actualPairs.Count.Should().Be(3);
            actualPairs.Should().NotContain(new Tuple<int, int>(1, 1));
            actualPairs.Should().NotContain(new Tuple<int, int>(2, 2));
            actualPairs.Should().NotContain(new Tuple<int, int>(3, 3));
        }
    }
}