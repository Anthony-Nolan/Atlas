using System;
using System.Threading.Tasks;
using FluentAssertions;
using Atlas.Utils.Core.Tasks;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Tasks
{
    [TestFixture]
    public class TaskUtilsTests
    {
        [Test]
        public void GivenAsyncAction_RunSync_RunsMethodSynchronously()
        {
            var hasRun = false;
            Action action = () => hasRun = true;

            Task.Factory.StartNew(action).RunSync();

            hasRun.Should().BeTrue();
        }

        [Test]
        public void GivenAsyncFunction_RunSync_RunsMethodSynchronously()
        {
            var task = Task.FromResult(true);

            task.RunSync().Should().BeTrue();
        }
    }
}
