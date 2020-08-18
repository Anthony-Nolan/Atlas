using System.Linq;
using Atlas.Common.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Utils
{
    public class FixedSizedQueueTests
    {
        [Test]
        public void FixedSizedQueue_WhenQueueFull_DropsFirstItemInQueueUponNewInsertion()
        {
            var fixedQueue = new FixedSizedQueue<int>(3);
            fixedQueue.Enqueue(1);
            fixedQueue.Enqueue(2);
            fixedQueue.Enqueue(3);
            fixedQueue.Enqueue(4);

            fixedQueue.Size.Should().Be(3);
            fixedQueue.ElementAt(0).Should().Be(2);
            fixedQueue.ElementAt(1).Should().Be(3);
            fixedQueue.ElementAt(2).Should().Be(4);
        }
    }
}