using System.Linq;
using FluentAssertions;
using Atlas.Utils.Core.Reflection;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Reflection
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void GivenInvalidSuffix_LoadNovaAssemblies_ReturnsEmptyList()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadNovaAssemblies("Unknown");

            ret.Should().BeEmpty();
        }

        [Test]
        public void GivenNoSuffix_LoadNovaAssemblies_LoadsAllReferencedAssembliesStartingWithNova()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadNovaAssemblies();

            ret.Select(a => a.GetName().Name).Should().Equal("Atlas.Utils.Test", "Atlas.Utils");
        }

        [Test]
        public void GivenValidSuffix_LoadNovaAssemblies_LoadsAllReferencedAssembliesStartingWithNova()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadNovaAssemblies("Utils.Test");

            ret.Select(a => a.GetName().Name).Should().Equal("Atlas.Utils.Test");
        }
    }
}
