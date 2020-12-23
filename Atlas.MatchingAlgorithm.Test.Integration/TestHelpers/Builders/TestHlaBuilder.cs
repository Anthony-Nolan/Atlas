using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    internal class TestHlaMetadata :  INullHandledHlaMatchingMetadata
    {
        public string LookupName { get; set; }

        public IList<string> MatchingPGroups { get; set; }
    }


    internal class TestHlaBuilder
    {
        private readonly TestHlaMetadata hlaMetadata;

        public TestHlaBuilder()
        {
            hlaMetadata = new TestHlaMetadata
            {
                LookupName = IncrementingIdGenerator.NextStringId("hla-lookup-"),
                MatchingPGroups = new List<string>()
            };
        }

        public TestHlaBuilder WithPGroups(params string[] pGroups)
        {
            hlaMetadata.MatchingPGroups = hlaMetadata.MatchingPGroups.Concat(pGroups).ToList();
            hlaMetadata.LookupName = pGroups.GetHashCode().ToString();
            return this;
        }
        
        public TestHlaMetadata Build()
        {
            return hlaMetadata;
        }
    }
}