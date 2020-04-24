using System.Reflection;
using FluentAssertions;
using Atlas.Utils.Core.Reflection;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Reflection
{
    [TestFixture]
    public class MemberExtensionsTests
    {
        [Test]
        public void GivenGetterMethod_GetReflectedInfo_ReturnsPropertyForGetter()
        {
            var propertyInfo = typeof(MethodInfo).GetProperty(nameof(MethodInfo.Name));
            var methodInfo = propertyInfo.GetMethod;

            methodInfo.GetReflectedInfo().Should().BeSameAs(propertyInfo);
        }

        [Test]
        public void GivenMethodInfo_GetReflectedInfo_ReturnsMethodInfoItself()
        {
            var methodInfo = typeof(object).GetMethod(nameof(GetHashCode));
            methodInfo.GetReflectedInfo().Should().BeSameAs(methodInfo);
        }
    }
}
