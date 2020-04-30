using System;
using System.Collections.Generic;
using FluentAssertions;
using Atlas.Utils.Core.Ordering;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Ordering
{
    [TestFixture]
    public class OrderingTests
    {
        [Test]
        public void GivenExplicitOrder_OrderItems_ItemsOrderedByGivenOrder()
        {
            var list = new List<int> { 1, 2, 3 };

            list.Sort(Order.Explicit(4, 2, 3, 1));

            list.Should().ContainInOrder(2, 3, 1);
        }

        [Test]
        public void GivenExplicitOrder_OrderUnknownItem_ThrowsIncomparableValue()
        {
            var list = new List<int> { 3, 4, 5 };

            list.Invoking(l => l.Sort(Order.Explicit(4, 2, 3, 1)))
                .ShouldThrow<InvalidOperationException>()
                .WithInnerException<IncomparableValueException>();
        }

        [Test]
        public void GivenFunctionalOrder_OrderItems_ItemsOrderedUsingInnerComparatorOnFunctionResult()
        {
            var list = new List<int> { 2, 3, 1 };

            list.Sort(Order.Natural<int>().OnResultOf<int>(i => -i));

            list.Should().BeInDescendingOrder();
        }

        [Test]
        public void GivenNaturalOrder_OrderItems_ReturnsItemsInComparableOrder()
        {
            var list = new List<int> { 2, 3, 1 };

            list.Sort(Order.Natural<int>());

            list.Should().BeInAscendingOrder();
        }
    }
}
