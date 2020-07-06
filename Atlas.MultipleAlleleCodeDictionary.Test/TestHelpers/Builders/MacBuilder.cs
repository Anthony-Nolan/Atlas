using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal static class MacBuilder
    {
        private static int macId = 0;

        private static int NextMacId => ++macId;
        
        public static Builder<Mac> New => Builder<Mac>.New
            .With(mac => mac.Hla, "default-hla")
            .WithFactory(mac => mac.Code, () => $"default-mac-{NextMacId}");
    }
}