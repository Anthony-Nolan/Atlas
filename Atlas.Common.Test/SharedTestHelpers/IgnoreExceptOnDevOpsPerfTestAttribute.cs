using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Atlas.Common.Test.SharedTestHelpers
{
    /// <summary>
    /// Allows us to define really slow tests that we don't want to run locally, but which do run on the nightly DevOps build.
    /// </summary>
    /// <summary>
    /// It's a conceptual derivative of the <see cref="IgnoreAttribute"/>.
    /// But <see cref="ApplyToTest"/> isn't virtual so we can't override it,
    /// so we're composed from an <see cref="IgnoreAttribute"/>, rather than inheriting from one.
    /// </summary>
    public class IgnoreExceptOnDevOpsPerfTestAttribute : NUnitAttribute, IApplyToTest
    {
        private const string reason = "We want some slow Tests that really exercise the Performance of the code, but they should only run on DevOps";
        public const string DevOpsIndicatorVariable = "IS_DEVOPS_PERF_TEST";

        // ReSharper disable once UnusedParameter.Local
        /// <param name="lastBenchmark">Please record the how long this test took last time you ran it.</param>
        public IgnoreExceptOnDevOpsPerfTestAttribute(string lastBenchmark)
        {}

        public void ApplyToTest(NUnit.Framework.Internal.Test test)
        {
            var isRunningDevOpsPerfTest = DoesEnvironmentVariableExist();
            if (!isRunningDevOpsPerfTest)
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
        private static bool DoesEnvironmentVariableExist()
        {
            //Don't really care *where* it's defined. So easier for devs if we just look everywhere.
            var userVariable = Environment.GetEnvironmentVariable(DevOpsIndicatorVariable, EnvironmentVariableTarget.User);
            var processVariable = Environment.GetEnvironmentVariable(DevOpsIndicatorVariable, EnvironmentVariableTarget.Process);
            var machineVariable = Environment.GetEnvironmentVariable(DevOpsIndicatorVariable, EnvironmentVariableTarget.Machine);
            var allVariables = userVariable ?? processVariable ?? machineVariable;
            return !string.IsNullOrWhiteSpace(allVariables);
        }
    }
}