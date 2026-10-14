using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Models
{
    [TestFixture]
    internal class AlleleInfoForMatchingTests
    {
        [Test]
        public void Equals_WhenOtherObjectIsOfAnotherType_ReturnsFalse()
        {
            var typing = new AlleleTyping(Locus.A, "01:01:01:01");
            var alleleInfo = new AlleleInfoForMatching(typing, typing, "01:01P", "01:01:01G");

            alleleInfo.Equals((object)typing).Should().BeFalse();
        }
    }
}
