using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.Dictionary
{
    [TestFixture]
    public class SerologyEntryExtensionsTest
    {
        private static object[] _serologyCollections =
        {
            new object[]
            {
                new List<SerologyTyping>{new SerologyTyping("A", "1", SerologySubtype.NotSplit)},
                new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit)}
            },
            new object[]
            {
                new List<SerologyTyping>
                {
                    new SerologyTyping("B", "51", SerologySubtype.Split),
                    new SerologyTyping("B", "5", SerologySubtype.Broad),
                    new SerologyTyping("B", "5102", SerologySubtype.Associated),
                    new SerologyTyping("B", "5103", SerologySubtype.Associated)
                },
                new List<SerologyEntry>
                {
                    new SerologyEntry("51", SerologySubtype.Split),
                    new SerologyEntry("5", SerologySubtype.Broad),
                    new SerologyEntry("5102", SerologySubtype.Associated),
                    new SerologyEntry("5103", SerologySubtype.Associated)
                }
            },
            new object[]
            {
                new List<SerologyTyping>
                {
                    new SerologyTyping("DR", "1", SerologySubtype.NotSplit),
                    new SerologyTyping("DR", "103", SerologySubtype.Associated)
                },
                new List<SerologyEntry>
                {
                    new SerologyEntry("1", SerologySubtype.NotSplit),
                    new SerologyEntry("103", SerologySubtype.Associated)
                }
            }
        };
        
        [TestCaseSource(nameof(_serologyCollections))]
        public void SerologyListConvertedToSerologyEntries(IEnumerable<SerologyTyping> serologyCollection, IEnumerable<SerologyEntry> expected)
        {
            var actual = serologyCollection.ToSerologyEntries();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
