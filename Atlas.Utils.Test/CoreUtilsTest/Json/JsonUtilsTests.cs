using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Schema;
using Atlas.Utils.Test.CoreUtils.Json;
using NUnit.Framework;

namespace Atlas.Utils.Test.CoreUtilsTest.Json
{
    [TestFixture]
    public class JsonUtilsTests
    {
        private static readonly Assembly Assembly = typeof(JsonUtilsTests).Assembly;

        [Test]
        public void GivenExistingEmbeddedResource_LoadSchema_ReturnsExpectedSchema()
        {
            var schema = JsonUtils.LoadSchemaFromResource(Assembly, "CoreUtilsTest/Resources/some-schema.json");

            schema.ShouldBeEquivalentTo(JSchema.Parse("{'type':'string'}"));
        }

        [Test]
        public async Task GivenExistingEmbeddedResource_LoadJsonContent_ReturnsExpectedContent()
        {
            var content = JsonUtils.LoadJsonContent(Assembly, "CoreUtilsTest/Resources/some-json.json");

            var ret = await content.ReadAsStringAsync();

            ret.Should().Be("{ \"key\": \"value\" }");
        }

        [Test]
        public void GivenNontExistingEmbeddedResource_LoadJsonContent_ThrowsArgException()
        {
            Action action = () => JsonUtils.LoadJsonContent(Assembly, "CoreUtilsTest/Resources/unknown.json");

            action.ShouldThrow<ArgumentException>().Which.Message
                .Should().Be("File CoreUtilsTest/Resources/unknown.json not found. Please make sure it's marked as an embedded resource.");
        }
    }
}
