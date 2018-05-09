using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
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
                new List<Serology>{new Serology("A", "1", SerologySubtype.NotSplit)},
                new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit)}
            },
            new object[]
            {
                new List<Serology>
                {
                    new Serology("B", "51", SerologySubtype.Split),
                    new Serology("B", "5", SerologySubtype.Broad),
                    new Serology("B", "5102", SerologySubtype.Associated),
                    new Serology("B", "5103", SerologySubtype.Associated)
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
                new List<Serology>
                {
                    new Serology("DR", "1", SerologySubtype.NotSplit),
                    new Serology("DR", "103", SerologySubtype.Associated)
                },
                new List<SerologyEntry>
                {
                    new SerologyEntry("1", SerologySubtype.NotSplit),
                    new SerologyEntry("103", SerologySubtype.Associated)
                }
            }
        };
        
        [TestCaseSource(nameof(_serologyCollections))]
        public void SerologyListConvertedToSerologyEntries(IEnumerable<Serology> serologyCollection, IEnumerable<SerologyEntry> expected)
        {
            var actual = serologyCollection.ToSerologyEntries();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
