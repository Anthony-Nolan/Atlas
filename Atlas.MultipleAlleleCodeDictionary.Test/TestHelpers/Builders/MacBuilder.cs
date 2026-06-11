using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using AutoFixture.Dsl;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;

internal static class MacBuilder
{
    private static int macId = 0;

    private static int NextMacId => ++macId;

    public static IPostprocessComposer<Mac> New => FixtureBuilder.For<Mac>()
        .With(mac => mac.Hla, "default-hla")
        .With(mac => mac.Code, () => $"default-mac-{NextMacId}");
}