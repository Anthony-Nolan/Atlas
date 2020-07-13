using System;

namespace Atlas.Common.Maths
{
    internal static class FactorialMaths
    {
        /// <remarks>
        /// Expected use case is when numerator is larger than denominator.
        /// </remarks>
        internal static long FactorialDivision(int topFactorial, int divisorFactorial)
        {
            if (divisorFactorial > topFactorial)
            {
                throw new NotImplementedException("Factorial division where denominator > numerator is not supported.");
            }

            long result = 1;
            for (var i = topFactorial; i > divisorFactorial; i--)
            {
                result *= i;
            }

            return result;
        }

        internal static long Factorial(int i)
        {
            if (i < 0)
            {
                throw new InvalidOperationException("Cannot calculate factorial of negative number");
            }
            return i <= 1 ? 1 : i * Factorial(i - 1);
        }
    }
}