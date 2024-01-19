using System.Linq;
using Atlas.Common.Utils.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.Reflection
{
    [TestFixture]
    public class AssemblyExtensionsTests
    {
        [Test]
        public void GivenInvalidSuffix_LoadAtlasAssemblies_ReturnsEmptyList()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadAtlasAssemblies("Unknown");

            ret.Should().BeEmpty();
        }

        [Test]
        public void GivenNoSuffix_LoadAtlasAssemblies_LoadsAllReferencedAssembliesStartingWithAtlas()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadAtlasAssemblies();

            ret.Select(a => a.GetName().Name).Should().Equal("Atlas.Common.Test", "Atlas.Common", "Atlas.Common.Public.Models", "Atlas.Client.Models");
        }

        [Test]
        public void GivenValidSuffix_LoadAtlasAssemblies_LoadsAllReferencedAssembliesStartingWithAtlas()
        {
            var ret = typeof(AssemblyExtensionsTests).Assembly.LoadAtlasAssemblies("Common.Test");

            ret.Select(a => a.GetName().Name).Should().Equal("Atlas.Common.Test");
        }
    }
}
