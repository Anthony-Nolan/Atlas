using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Atlas.Utils.Test.CoreUtils.Assertions
{
    public static class JTokenAssertionsExtensions
    {
        public static JTokenAssertions Should(this JToken subject)
        {
            return new JTokenAssertions(subject);
        }
    }

    public class JTokenAssertions : CollectionAssertions<JToken, JTokenAssertions>
    {
        public JTokenAssertions(JToken subject)
        {
            Subject = subject;
        }

        protected override string Context { get; } = nameof(JToken);

        public AndConstraint<JTokenAssertions> HaveValue<T>(string jpath, T expected, string because = "",
            params object[] becauseArgs)
        {
            AssertProperyExists(jpath, because, becauseArgs)
                .Then
                .Given(() => Subject.SelectToken(jpath).Value<T>())
                .ForCondition(val => val.IsSameOrEqualTo(expected))
                .FailWith("Expected {0} at path {1}{reason} but found {2}.", actual => expected, actual => jpath,
                    actual => actual);
            return new AndConstraint<JTokenAssertions>(this);
        }

        public AndWhichConstraint<JTokenAssertions, IEnumerable<T>> HaveValues<T>(string path, params T[] expected)
        {
            return HaveValues(path, expected, string.Empty);
        }

        public AndWhichConstraint<JTokenAssertions, IEnumerable<T>> HaveValues<T>(string path, IEnumerable<T> expected,
            string because = "", params object[] becauseArgs)
        {
            AssertProperyExists(path, because, becauseArgs);
            var values = Subject.SelectTokens(path).Values<T>().ToList();
            values.Should().Equal(expected, because, becauseArgs);
            return new AndWhichConstraint<JTokenAssertions, IEnumerable<T>>(this, values);
        }

        public AndConstraint<JTokenAssertions> MatchSchema(JSchema schema, string because = "",
            params object[] becauseArgs)
        {
            Execute.Assertion.Given(() =>
                {
                    IList<string> errors;
                    var valid = Subject.IsValid(schema, out errors);
                    return Tuple.Create(valid, errors);
                })
                .ForCondition(val => val.Item1)
                .FailWith("Expected content to match schema{reason}, but had errors: {0}",
                    actual => string.Join("; ", actual.Item2.ToArray()));
            return new AndConstraint<JTokenAssertions>(this);
        }

        private Continuation AssertProperyExists(string jpath, string because = "", params object[] becauseArgs)
        {
            var token = Subject.SelectTokens(jpath);
            return Execute.Assertion
                .ForCondition(token.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith("Property at path {0} does not exist{reason}.", jpath);
        }
    }
}
