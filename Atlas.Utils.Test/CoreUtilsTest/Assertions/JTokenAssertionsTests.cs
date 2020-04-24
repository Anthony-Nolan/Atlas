using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Atlas.Utils.Test.CoreUtils.Assertions;
using NUnit.Framework;

namespace Nova.Utils.Test.CoreUtilsTest.Assertions
{
    [TestFixture]
    public class JTokenAssertionsTests
    {
        #region HaveValue

        [Test]
        public void GivenJsonObjectWithCorrectValue_ShouldHaveValue_DoesNotThrow()
        {
            var token = JToken.Parse("{'key': 'value'}");

            Action action = () => token.Should().HaveValue("$.key", "value");

            action.ShouldNotThrow();
        }

        [Test]
        public void GivenJsonObjectWithoutKey_ShouldHaveValue_Throws()
        {
            var token = JToken.Parse("{}");

            Action action = () => token.Should().HaveValue("$.key", "value");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Property at path \"$.key\" does not exist.");
        }

        [Test]
        public void GivenJsonObjectWithoutKeyWithReason_ShouldHaveValue_Throws()
        {
            var token = JToken.Parse("{}");

            Action action = () => token.Should().HaveValue("$.key", "value", "some {0} reason", "parameterised");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Property at path \"$.key\" does not exist because some parameterised reason.");
        }

        [Test]
        public void GivenJsonObjectWithoutCorrectValue_ShouldHaveValue_Throws()
        {
            var token = JToken.Parse("{'key': 'incorrect'}");

            Action action = () => token.Should().HaveValue("$.key", "value");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected \"value\" at path \"$.key\" but found \"incorrect\".");
        }

        [Test]
        public void GivenJsonObjectWithoutCorrectValueWithReason_ShouldHaveValue_Throws()
        {
            var token = JToken.Parse("{'key': 'incorrect'}");

            Action action = () => token.Should().HaveValue("$.key", "value", "some {0} reason", "parameterised");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected \"value\" at path \"$.key\" because some parameterised reason but found \"incorrect\".");
        }

        #endregion

        #region HaveValues

        [Test]
        public void GivenJsonObjectWithCorrectValue_ShouldHaveValues_DoesNotThrow()
        {
            var token = JToken.Parse("{'key': ['value1','value2']}");

            Action action = () => token.Should().HaveValues("$.key", "value1", "value2");

            action.ShouldNotThrow();
        }

        [Test]
        public void GivenJsonObjectWithoutKey_ShouldHaveValues_Throws()
        {
            var token = JToken.Parse("{}");

            Action action = () => token.Should().HaveValues("$.key", "value1", "value2");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Property at path \"$.key\" does not exist.");
        }

        [Test]
        public void GivenJsonObjectWithoutKeyWithReason_ShouldHaveValues_Throws()
        {
            var token = JToken.Parse("{}");

            Action action = () => token.Should().HaveValues("$.key", new[] { "value" }, "some {0} reason", "parameterised");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Property at path \"$.key\" does not exist because some parameterised reason.");
        }

        [Test]
        public void GivenJsonObjectWithoutCorrectValue_ShouldHaveValues_Throws()
        {
            var token = JToken.Parse("{'key': ['incorrect']}");

            Action action = () => token.Should().HaveValues("$.key", "correct");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected collection to be equal to {\"correct\"}, but {\"incorrect\"} differs at index 0.");
        }

        [Test]
        public void GivenJsonObjectWithoutCorrectValueWithReason_ShouldHaveValues_Throws()
        {
            var token = JToken.Parse("{'key': ['incorrect']}");

            Action action = () => token.Should().HaveValues("$.key", new[] { "correct" }, "some {0} reason", "parameterised");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected collection to be equal to {\"correct\"} because some parameterised reason, but {\"incorrect\"} differs at index 0.");
        }

        #endregion

        #region MatchSchema

        [Test]
        public void GivenJsonObjectAndMatchingSchema_MatchSchema_ShouldNotThrow()
        {
            var json = JToken.Parse("{'key': 'value'}");
            var schema = JSchema.Parse("{'type':'object', 'properties': {'key': {'type':'string'}}}");

            Action action = () => json.Should().MatchSchema(schema);

            action.ShouldNotThrow();
        }

        [Test]
        public void GivenJsonObjectAndNonMatchingSchema_MatchSchema_ShouldThrow()
        {
            var json = JToken.Parse("{'key': 123}");
            var schema = JSchema.Parse("{'type':'object', 'properties': {'key': {'type':'string'}}}");

            Action action = () => json.Should().MatchSchema(schema);

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected content to match schema, but had errors: \"Invalid type. Expected String but got Integer. Path 'key', line 1, position 11.\"");
        }

        #endregion
    }
}
