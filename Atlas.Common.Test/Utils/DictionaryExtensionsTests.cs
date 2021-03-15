using System;
using System.Collections.Generic;
using Atlas.Common.Utils.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Utils
{
    [TestFixture]
    internal class DictionaryExtensionsTests
    {
        private const string DefaultKey = "key";
        private const string DefaultValue = "value";

        [Test]
        public void Merge_WithSameKeyValuePairInBoth_KeepsKeyValuePair()
        {
            var dict1 = new Dictionary<string, string> {{DefaultKey, DefaultValue}};
            var dict2 = new Dictionary<string, string> {{DefaultKey, DefaultValue}};

            var merged = dict1.Merge(dict2);

            merged[DefaultKey].Should().Be(DefaultValue);
        }
        
        [Test]
        public void Merge_WithDifferentKeyValuePairsInEach_KeepsAllValues()
        {
            const string key2 = "key2";
            const string value2 = "value2";
            
            var dict1 = new Dictionary<string, string> {{DefaultKey, DefaultValue}};
            var dict2 = new Dictionary<string, string> {{key2, value2}};

            var merged = dict1.Merge(dict2);

            merged[DefaultKey].Should().Be(DefaultValue);
            merged[key2].Should().Be(value2);
        }

        [Test]
        public void Merge_WithSameKeyAndDifferentValues_ThrowsException()
        {   
            var dict1 = new Dictionary<string, string> {{DefaultKey, DefaultValue}};
            var dict2 = new Dictionary<string, string> {{DefaultKey, "some-other-value"}};

            dict1.Invoking(d => d.Merge(dict2)).Should().Throw<ArgumentException>();
        }
    }
}