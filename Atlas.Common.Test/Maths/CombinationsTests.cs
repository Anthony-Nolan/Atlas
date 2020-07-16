using System;
using System.Diagnostics;
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
            var array = new[] { 1, 2 };

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

        [Test, // most of these a < 0.1s
         TestCase(1, 1),
         TestCase(2, 1),
         TestCase(3, 1),
         TestCase(4, 1),
         TestCase(2, 2),
         TestCase(3, 2),
         TestCase(4, 2),
         TestCase(3, 3),
         TestCase(4, 3),
         TestCase(4, 4),

         TestCase(7, 5),

         TestCase(8, 3),
         TestCase(9, 7),
         TestCase(10, 10),
         TestCase(11, 3),
         TestCase(13, 1),
         TestCase(9, 5),
         TestCase(10, 5),
         TestCase(20, 3), // ~0.5 s
         TestCase(20, 4), // ~6.2 s
         TestCase(15, 5), // ~2.5 s
        ]
        public void AllCombinations_ReturnedCollectionsHaveExpectedProperties(int n, int r)
        {
            var combinations = Combinations.AllCombinations(n, r).ToList();

            var expectedNumberOfCombinations = (int)Combinations.nCr(n, r);
            combinations.Should().HaveCount(expectedNumberOfCombinations);
            foreach (var combination in combinations)
            {
                combination.Should().HaveCount(r);
                combination.Should().OnlyHaveUniqueItems();
                combination.Should().BeInAscendingOrder();

                foreach (var otherCombination in combinations)
                {
                    if (ReferenceEquals(otherCombination,combination))
                    {
                        continue;
                    }

                    ShouldNotHaveIdenticalValues(combination, otherCombination);
                }
            }
        }

        private static void ShouldNotHaveIdenticalValues(int[] first, int[] second)
        {
            var differenceFound = false;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    differenceFound = true;
                    break;
                }
            }

            differenceFound.Should().BeTrue();
        }

        [Test]
        public void AllCombinations_ReturnsExpectedResults()
        {
            ((Action)(() => Combinations.AllCombinations(0, 0).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            ((Action)(() => Combinations.AllCombinations(0, 1).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(0, 2).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(0, 3).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(0, 4).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            ((Action)(() => Combinations.AllCombinations(1, 0).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(2, 0).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(3, 0).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(4, 0).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            Combinations.AllCombinations(1, 1).Should().BeEquivalentTo(new[] { new[] { 0 } });
            Combinations.AllCombinations(2, 1).Should().BeEquivalentTo(new[] { new[] { 0 }, new[] { 1 } });
            Combinations.AllCombinations(3, 1).Should().BeEquivalentTo(new[] { new[] { 0 }, new[] { 1 }, new[] { 2 } });
            Combinations.AllCombinations(4, 1).Should().BeEquivalentTo(new[] { new[] { 0 }, new[] { 1 }, new[] { 2 }, new[] { 3 } });

            ((Action)(() => Combinations.AllCombinations(1, 2).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(1, 3).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(1, 4).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            Combinations.AllCombinations(2, 2).Should().BeEquivalentTo(new[] { new[] { 0, 1 } });
            Combinations.AllCombinations(3, 2).Should().BeEquivalentTo(new[] { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 2 } });
            Combinations.AllCombinations(4, 2).Should().BeEquivalentTo(new[] { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 }, new[] { 1, 2 }, new[] { 1, 3 }, new[] { 2, 3 } });

            ((Action)(() => Combinations.AllCombinations(2, 3).ToList())).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => Combinations.AllCombinations(2, 4).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            Combinations.AllCombinations(3, 3).Should().BeEquivalentTo(new[] { new[] { 0, 1, 2 } });
            Combinations.AllCombinations(4, 3).Should().BeEquivalentTo(new[] { new[] { 0, 1, 2 }, new[] { 0, 1, 3 }, new[] { 0, 2, 3 }, new[] { 1, 2, 3 } });

            ((Action)(() => Combinations.AllCombinations(3, 4).ToList())).Should().Throw<ArgumentOutOfRangeException>();

            Combinations.AllCombinations(4, 4).Should().BeEquivalentTo(new[] { new[] { 0, 1, 2, 3 } });

            Combinations.AllCombinations(7, 5).Should().BeEquivalentTo(new[] { 
                new[] {0,1,2,3,4},
                new[] {0,1,2,3,5},
                new[] {0,1,2,3,6},
                new[] {0,1,2,4,5},
                new[] {0,1,2,4,6},
                new[] {0,1,2,5,6},
                new[] {0,1,3,4,5},
                new[] {0,1,3,4,6},
                new[] {0,1,3,5,6},
                new[] {0,1,4,5,6},
                new[] {0,2,3,4,5},
                new[] {0,2,3,4,6},
                new[] {0,2,3,5,6},
                new[] {0,2,4,5,6},
                new[] {0,3,4,5,6},
                new[] {1,2,3,4,5},
                new[] {1,2,3,4,6},
                new[] {1,2,3,5,6},
                new[] {1,2,4,5,6},
                new[] {1,3,4,5,6},
                new[] {2,3,4,5,6},
                });
        }
    }
}