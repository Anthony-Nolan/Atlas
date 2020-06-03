using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedAlleleNameMetadata : IAlleleNameMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => CurrentAlleleNames;
        public List<string> CurrentAlleleNames { get; }

        public FileBackedAlleleNameMetadata(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            List<string> currentAlleleNames)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            CurrentAlleleNames = currentAlleleNames;
        }
    }
}