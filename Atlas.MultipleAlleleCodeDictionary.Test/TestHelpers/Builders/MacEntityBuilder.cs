using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using LochNessBuilder;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal class MacEntityBuilder
    {
        private static int macId = 0;

        private static int NextMacId => ++macId;
        
        public static Builder<MultipleAlleleCodeEntity> New => Builder<MultipleAlleleCodeEntity>.New
            .With(mac => mac.HLA, "default-hla")
            .With(mac => mac.RowKey, $"default-mac-{NextMacId}");
    }
}