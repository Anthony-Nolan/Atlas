using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using LochNessBuilder;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal class MacEntityBuilder
    {
        private static int macId = 0;

        private static int NextMacId => ++macId;
        
        public static Builder<MacEntity> New => Builder<MacEntity>.New
            .With(mac => mac.HLA, "default-hla")
            .With(mac => mac.RowKey, $"default-mac-{NextMacId}");
    }
}