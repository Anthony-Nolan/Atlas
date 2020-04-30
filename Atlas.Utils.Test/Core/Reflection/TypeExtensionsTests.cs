using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Atlas.Utils.Core.Reflection;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Reflection
{
    [TestFixture]
    public class TypeExtensionsTests
    {
        [TestCase(typeof(Type), typeof(Type), true)]
        [TestCase(typeof(MemberInfo), typeof(Type), true)]
        [TestCase(typeof(Type), typeof(MemberInfo), false)]
        [TestCase(typeof(List<>), typeof(List<object>), true)]
        [TestCase(typeof(IList<>), typeof(List<object>), true)]
        [TestCase(typeof(IList<>), typeof(IList<object>), true)]
        public void GivenTypes_IsAssignableFromGeneric_BehavesAsExpected(Type to, Type from, bool expected)
        {
            to.IsAssignableFromGeneric(from).Should().Be(expected);
        }

        [TestCase(typeof(IEnumerable<int>), typeof(IEnumerable<>), typeof(IEnumerable<int>))]
        [TestCase(typeof(IList<int>), typeof(IEnumerable<>), typeof(IEnumerable<int>))]
        [TestCase(typeof(List<int>), typeof(IEnumerable<>), typeof(IEnumerable<int>))]
        [TestCase(typeof(IDictionary<string, int>), typeof(IEnumerable<>), typeof(IEnumerable<KeyValuePair<string, int>>))]
        [TestCase(typeof(IDictionary<string, int>), typeof(IDictionary<,>), typeof(IDictionary<string, int>))]
        [TestCase(typeof(Dictionary<string, int>), typeof(IDictionary<,>), typeof(IDictionary<string, int>))]
        [TestCase(typeof(Dictionary<string, int>), typeof(IEnumerable<>), typeof(IEnumerable<KeyValuePair<string, int>>))]
        [TestCase(typeof(IDictionary<string, int>), typeof(IList<>), null)]
        [TestCase(typeof(Dictionary<string, int>), typeof(IList<>), null)]
        public void GivenTypeAndInterface_GetInterfaceMatchingGeneric_ReturnsConcreteInterface(Type type, Type generic, Type expected)
        {
            if (expected == null)
            {
                type.GetInterfaceMatchingGeneric(generic).Should().BeNull();
            }
            else
            {
                type.GetInterfaceMatchingGeneric(generic).Should().Be(expected);
            }
        }

        [Test]
        public void GivenTypeAndNonInterace_GetInterfaceMatchingGeneric_ThrowsArgumentException()
        {
            typeof(object).Invoking(t => t.GetInterfaceMatchingGeneric(typeof(object))).ShouldThrow<ArgumentException>();
        }

        [Test]
        public void GivenTypeAndNullInterface_GetInterfaceMatchingGeneric_ThrowsArgumentNullException()
        {
            typeof(object).Invoking(t => t.GetInterfaceMatchingGeneric(null)).ShouldThrow<ArgumentNullException>();
        }
    }
}
