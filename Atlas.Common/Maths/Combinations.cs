using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.Maths
{
    // TODO: ATLAS-400: Review code lifted from StackOverflow
    public static class Combinations
    {
        public static long NumberOfPairs(int n, bool shouldIncludeSelfPairs = false)
        {
            var selfPairs = n;
            var nonSelfPairs = nCr(n, 2);
            return shouldIncludeSelfPairs ? nonSelfPairs + selfPairs : nonSelfPairs;
        }
        
        // ReSharper disable once InconsistentNaming
        public static long nCr(int n, int r)
        {
            // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
            return nPr(n, r) / Factorial(r);
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

        private static IEnumerable<T[]> AllCombinations<T>(T[] array, int m)
        {
            var result = new T[m];
            foreach (var j in CombinationsRosettaWoRecursion(m, array.Length))
            {
                for (var i = 0; i < m; i++)
                {
                    result[i] = array[j[i]];
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
        private static IEnumerable<int[]> CombinationsRosettaWoRecursion(int m, int n)
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

        private static IEnumerable<Tuple<T, T>> AllSelfPairs<T>(IEnumerable<T> array)
        {
            return array.Select(x => new Tuple<T, T>(x, x));
        }

        // ReSharper disable once InconsistentNaming
        private static long nPr(int n, int r)
        {
            // naive: return Factorial(n) / Factorial(n - r);
            return FactorialDivision(n, n - r);
        }

        private static long FactorialDivision(int topFactorial, int divisorFactorial)
        {
            long result = 1;
            for (var i = topFactorial;
                i > divisorFactorial;
                i--)
            {
                result *= i;
            }

            return result;
        }

        private static long Factorial(int i)
        {
            return i <= 1 ? 1 : i * Factorial(i - 1);
        }
    }
}