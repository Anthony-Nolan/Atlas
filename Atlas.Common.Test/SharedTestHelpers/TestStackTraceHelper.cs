using System;
using System.Threading.Tasks;

namespace Atlas.Common.Test.SharedTestHelpers
{
    public static class TestStackTraceHelper
    {
        /// <summary>
        /// Wrap the contents of any <see cref="OneTimeSetUp"/> methods in this, to
        /// ensure that useful stackTraces are provided in the event of any errors.
        /// </summary>
        /// <remarks>
        /// See here for NUnit insisting that they're in the right:
        /// https://github.com/nunit/nunit3-vs-adapter/issues/671
        /// </remarks>
        public static void CatchAndRethrowWithStackTraceInExceptionMessage(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString(), e);
            }
        }

        /// <inheritdoc cref="CatchAndRethrowWithStackTraceInExceptionMessage"/>
        public static async Task CatchAndRethrowWithStackTraceInExceptionMessage_Async(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString(), e);
            }
        }
    }
}