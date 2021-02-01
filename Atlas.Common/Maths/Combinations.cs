using System;
using System.Collections.Generic;
using System.Linq;
using static Atlas.Common.Maths.FactorialMaths;

namespace Atlas.Common.Maths
{
    // Libraries exist that perform the work we do here: e.g. https://github.com/eoincampbell/combinatorics
    // IF we ever start having trouble with this combination code, consider swapping in a Nuget package to do this
    public static class Combinations
    {
        /// <summary>
        /// Calculates how many pairs exist for a collection of size n
        /// </summary>
        /// <param name="n"></param>
        /// <param name="shouldIncludeSelfPairs">
        /// If <c>true</c>, includes the number of pairs consisting of the same element twice.
        /// Otherwise, each element can only be paired with distinct other elements.
        /// </param>
        /// <returns></returns>
        public static long NumberOfPairs(int n, bool shouldIncludeSelfPairs = false)
        {
            if (n < 0)
            {
                throw new InvalidOperationException("A collection cannot have a negative length. Cannot count number of pairs.");
            }

            var selfPairs = n;
            var nonSelfPairs = nCr(n, 2);
            return shouldIncludeSelfPairs ? nonSelfPairs + selfPairs : nonSelfPairs;
        }

        public static long NumberOfPairsOfCartesianProduct<T>(ICollection<T> collection1, ICollection<T> collection2) => collection1.Count * collection2.Count;

        /// <param name="collection">Array of initial values to form pairs from.</param>
        /// <param name="shouldIncludeSelfPairs">
        /// When false, returns pairs of items in the list.
        /// When true, returns combinations *including* each item paired with itself.
        /// </param>
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(IEnumerable<T> collection, bool shouldIncludeSelfPairs = false)
        {
            var array = collection.ToArray();
            var empty = Enumerable.Empty<Tuple<T, T>>();
            if (!array.Any())
            {
                return empty;
            }

            if (array.Count() == 1)
            {
                var single = array.Single();
                return shouldIncludeSelfPairs ? new []{ Tuple.Create(single, single) } : empty;
            }

            var nonSelfPairs = AllCombinations(array, 2).Select(p => new Tuple<T, T>(p[0], p[1]));
            return shouldIncludeSelfPairs ? nonSelfPairs.Concat(AllSelfPairs(array)) : nonSelfPairs;
        }

        /// <summary>
        /// Enumerate all possible m-sized combinations of given collection.
        /// Does not include repetitions - each item cannot be in a combination with itself.  
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="r_combinationSize"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        private static IEnumerable<IReadOnlyList<T>> AllCombinations<T>(IReadOnlyList<T> collection, int r_combinationSize)
        {
            var allIndexCombinations = AllCombinations(collection.Count, r_combinationSize).ToList();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var indexCombination in allIndexCombinations)
            {
                // Given collection {a, b, c} and index combination {1, 2}, returns {b, c}
                yield return indexCombination.Select(i => collection[i]).ToList();
            }
        }

        /// <summary>
        /// This is the algorithm for generating combinatorics without recursion, taken from Rosetta code:https://rosettacode.org/wiki/Combinations#C.23
        ///
        /// From Rosetta code: Given non-negative integers r & n, generate all size r combinations of the integers from 0 to n-1 in sorted order  
        /// </summary>
        /// <param name="n_sourceCollectionSize"></param>
        /// <param name="r_combinationSize"></param>
        // ReSharper disable twice InconsistentNaming
        public static IEnumerable<int[]> AllCombinations(int n_sourceCollectionSize, int r_combinationSize)
        {
            if (n_sourceCollectionSize * r_combinationSize <= 0 || n_sourceCollectionSize <= 0)
            {
                throw new ArgumentOutOfRangeException($"Both 'n' and 'r' must be strictly positive.  r was '{r_combinationSize}', n was '{n_sourceCollectionSize}'.");
            }

            if (r_combinationSize > n_sourceCollectionSize)
            {
                throw new ArgumentOutOfRangeException(nameof(r_combinationSize), $"'r' may not be greater than 'n'. r was '{r_combinationSize}', n was '{n_sourceCollectionSize}'.");
            }

            var result = new int[r_combinationSize];
            var stack = new Stack<int>(r_combinationSize);

            stack.Push(0);
            while (stack.Count > 0)
            {
                var index = stack.Count - 1;
                var value = stack.Pop();
                while (value < n_sourceCollectionSize)
                {
                    //"value++;" rather than "++value;", because we want the values in this collection to be 0-indexed not 1-indexed.
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != r_combinationSize)
                    {
                        continue;
                    }

                    yield return (int[]) result.Clone();
                    break;
                }
            }
        }

        /// <returns>
        /// All elements of the collection as pairs with itself.
        /// e.g. [1, 2] => [[1,1], [2,2]]
        /// </returns>
        private static IEnumerable<Tuple<T, T>> AllSelfPairs<T>(IEnumerable<T> array)
        {
            return array.Select(x => new Tuple<T, T>(x, x));
        }

        // ReSharper disable once InconsistentNaming
        public static long nCr(int n_sourceCollectionSize, int r_combinationSize)
        {
            // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
            return nPr(n_sourceCollectionSize, r_combinationSize) / Factorial(r_combinationSize);
        }

        // ReSharper disable once InconsistentNaming
        private static long nPr(int n_sourceCollectionSize, int r_combinationSize)
        {
            // naive: return Factorial(n) / Factorial(n - r);
            return FactorialDivision(n_sourceCollectionSize, n_sourceCollectionSize - r_combinationSize);
        }
    }
}