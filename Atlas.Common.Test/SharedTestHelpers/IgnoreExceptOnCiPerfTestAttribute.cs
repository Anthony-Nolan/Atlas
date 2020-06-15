using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Atlas.Common.Test.SharedTestHelpers
{
    /// <summary>
    /// Allows us to define really slow tests that we don't want to run locally, but which do run on the nightly DevOps build.
    /// </summary>
    public class IgnoreExceptOnCiPerfTestAttribute : IgnoreUnlessAttribute
    {
        private const string Reason = "We want some slow Tests that really exercise the Performance of the code, but they should only run on DevOps";
        public const string CiIndicatorVariable = "RUN_CI_PERF_TESTS";

        // ReSharper disable once UnusedParameter.Local
        /// <param name="lastBenchmark">Please record the how long this test took last time you ran it.</param>
        public IgnoreExceptOnCiPerfTestAttribute(string lastBenchmark) : base(CiIndicatorVariable, Reason)
        { }
    }

    /// <summary>
    /// Ignore a test based on presence or absence of an Environment Variable.
    /// </summary>
    /// <remarks>
    /// It's a conceptual derivative of the <see cref="IgnoreAttribute"/>.
    /// But <see cref="ApplyToTest"/> isn't virtual so we can't override it,
    /// so we're composed from an <see cref="IgnoreAttribute"/>, rather than inheriting from one.
    /// </remarks>
    public class IgnoreUnlessAttribute : NUnitAttribute, IApplyToTest
    {
        private readonly string environmentVariableToConditionUpon;
        private readonly string reason;

        /// <param name="environmentVariableToConditionUpon">If this string exists as an Environment Variable, run the test. Otherwise <see cref="IgnoreAttribute"/> it.</param>
        /// <param name="reason">Text to display if the test ends up being Ignored.</param>
        public IgnoreUnlessAttribute(string environmentVariableToConditionUpon, string reason)
        {
            this.environmentVariableToConditionUpon = environmentVariableToConditionUpon;
            this.reason = reason;
        }

        public void ApplyToTest(NUnit.Framework.Internal.Test test)
        {
            if (!SpecifiedEnvironmentVariableExists())
            {
                IgnoreTest(test);
            }
        }

        private void IgnoreTest(NUnit.Framework.Internal.Test test)
        {
            var ignoreAttribute = new IgnoreAttribute(reason);
            ignoreAttribute.ApplyToTest(test);
        }

        /// <summary>
        /// Checks whether the relevant variable is defined anywhere that we can see.
        /// </summary>
        private bool SpecifiedEnvironmentVariableExists()
        {
            //Don't really care *where* it's defined. So easier for devs if we just look everywhere.
            var userVariable = Environment.GetEnvironmentVariable(environmentVariableToConditionUpon, EnvironmentVariableTarget.User);
            var processVariable = Environment.GetEnvironmentVariable(environmentVariableToConditionUpon, EnvironmentVariableTarget.Process);
            var machineVariable = Environment.GetEnvironmentVariable(environmentVariableToConditionUpon, EnvironmentVariableTarget.Machine);
            var allVariables = userVariable ?? processVariable ?? machineVariable;
            return !string.IsNullOrWhiteSpace(allVariables);
        }
    }
}