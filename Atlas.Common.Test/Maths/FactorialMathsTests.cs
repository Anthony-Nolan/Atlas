using System;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.Common.Maths.FactorialMaths;

namespace Atlas.Common.Test.Maths
{
    [TestFixture]
    internal class FactorialMathsTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(6, 720)]
        public void Factorial_CalculatesFactorial(int input, int expectedOutput)
        {
            Factorial(input).Should().Be(expectedOutput);
        }
        
        [Test]
        public void Factorial_ForNegativeInput_ThrowsException()
        {
            Func<long> action = () => Factorial(-2);
            action.Should().Throw<InvalidOperationException>();
        }
        
        [Test]
        public void FactorialDivision_CalculatesCorrectValue()
        {
            // Use very large numbers to ensure implementation can cope - calculating actual factorials of these numbers is infeasible.
            FactorialDivision(10001, 10000).Should().Be(10001);
        }
        
        [Test]
        public void FactorialDivision_WhenDenominatorLargerThanNumerator_ThrowsException()
        {
            Func<long> action = () => FactorialDivision(100, 9999);

            action.Should().Throw<NotImplementedException>();
        }
    }
}