using System;
using System.Collections.Generic;
using System.Linq;
using static Atlas.Common.Maths.FactorialMaths;

namespace Atlas.Common.Maths
{
    public static class Combinations
    {
        /// <summary>
        /// Calculates how many pairs exist for a collection of size n
        /// </summary>
        /// <param name="n"></param>
        /// <param name="shouldIncludeSelfPairs">
        /// If set, includes the number of pairs consisting of the same element twice.
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

        /// <param name="array">Array of initial values to form pairs from.</param>
        /// <param name="shouldIncludeSelfPairs">
        /// When false, returns pairs of items in the list.
        /// When true, returns combinations *including* each item paired with itself.
        /// </param>
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(T[] array, bool shouldIncludeSelfPairs = false)
        {
            var nonSelfPairs = AllCombinations(array, 2).Select(p => new Tuple<T, T>(p[0], p[1]));
            return shouldIncludeSelfPairs ? nonSelfPairs.Concat(AllSelfPairs(array)) : nonSelfPairs;
        }

        /// <summary>
        /// Enumerate all possible m-sized combinations of given collection.
        /// Does not include repetitions - each item cannot be in a combination with itself.  
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="m"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static IEnumerable<T[]> AllCombinations<T>(IReadOnlyList<T> collection, int m)
        {
            var result = new T[m];
            foreach (var j in AllCombinations(m, collection.Count))
            {
                for (var i = 0; i < m; i++)
                {
                    result[i] = collection[j[i]];
                }

                yield return result;
            }
        }

        /// <summary>
        /// Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        /// in lexicographic order (first [0, 1, 2, ..., m-1]). 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        private static IEnumerable<int[]> AllCombinations(int m, int n)
        {
            var result = new int[m];
            var stack = new Stack<int>(m);
            stack.Push(0);
            while (stack.Count > 0)
            {
                var index = stack.Count - 1;
                var value = stack.Pop();
                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != m)
                    {
                        continue;
                    }

                    yield return (int[]) result.Clone();
                    break;
                }
            }
        }

        /// <returns>
        /// All elements of the array as pairs with itself.
        /// e.g. [1, 2] => [[1,1], [2,2]]
        /// </returns>
        private static IEnumerable<Tuple<T, T>> AllSelfPairs<T>(IEnumerable<T> array)
        {
            return array.Select(x => new Tuple<T, T>(x, x));
        }

        // ReSharper disable once InconsistentNaming
        private static long nCr(int n, int r)
        {
            // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
            return nPr(n, r) / Factorial(r);
        }

        // ReSharper disable once InconsistentNaming
        private static long nPr(int n, int r)
        {
            // naive: return Factorial(n) / Factorial(n - r);
            return FactorialDivision(n, n - r);
        }
    }
}