using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    internal class TestHlaMetadata : IHlaMatchingMetadata
    {
        public string OriginalName { get; set; }
        public string LookupName { get; set; }
        public IList<string> MatchingPGroups { get; set; }
        public Locus Locus { get; set; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public string SerialisedHlaInfoType { get; }
        public bool IsNullExpressingTyping { get; }
    }


    internal class TestHlaBuilder
    {
        private readonly TestHlaMetadata hlaMetadata;

        public TestHlaBuilder()
        {
            hlaMetadata = new TestHlaMetadata
            {
                Locus = Locus.A,
                LookupName = IncrementingIdGenerator.NextStringId("hla-lookup-"),
                OriginalName = IncrementingIdGenerator.NextStringId("hla-original-"),
                MatchingPGroups = new List<string>()
            };
        }

        public TestHlaBuilder WithPGroups(params string[] pGroups)
        {
            hlaMetadata.MatchingPGroups = hlaMetadata.MatchingPGroups.Concat(pGroups).ToList();
            hlaMetadata.OriginalName = pGroups.GetHashCode().ToString();
            return this;
        }
        
        public TestHlaMetadata Build()
        {
            return hlaMetadata;
        }
    }
}