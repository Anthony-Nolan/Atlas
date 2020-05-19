using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class TestHla : IHlaMatchingLookupResult
    {
        public string OriginalName { get; set; }
        public string LookupName { get; set; }
        public IEnumerable<string> MatchingPGroups { get; set; }
        public Locus Locus { get; set; }

        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public bool IsNullExpressingTyping { get; }
        public HlaLookupTableEntity ConvertToTableEntity()
        {
            throw new NotImplementedException();
        }
    }


    public class TestHlaBuilder
    {
        private readonly TestHla hla;

        public TestHlaBuilder()
        {
            hla = new TestHla
            {
                Locus = Locus.A,
                LookupName = "HLA",
                OriginalName = "HLA",
                MatchingPGroups = new List<string>()
            };
        }

        public TestHlaBuilder WithPGroups(params string[] pGroups)
        {
            hla.MatchingPGroups = hla.MatchingPGroups.Concat(pGroups);
            return this;
        }
        
        public TestHla Build()
        {
            return hla;
        }
    }
}