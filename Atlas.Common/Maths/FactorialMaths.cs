using System;

namespace Atlas.Common.Maths
{
    internal static class FactorialMaths
    {
        /// <remarks>
        /// Calculates (a!/b!).
        /// 
        /// Expected use case is when numerator is larger than denominator.
        /// </remarks>
        internal static long FactorialDivision(int numeratorFactorial, int denominatorFactorial)
        {
            if (denominatorFactorial > numeratorFactorial)
            {
                throw new NotImplementedException("Factorial division where denominator > numerator is not supported.");
            }

            long result = 1;
            for (var i = numeratorFactorial; i > denominatorFactorial; i--)
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
            return i == 0 ? 1 : i * Factorial(i - 1);
        }
    }
}