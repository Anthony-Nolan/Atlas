using AutoFixture;
using AutoFixture.Dsl;

namespace Atlas.Common.Test.SharedTestHelpers.Builders;

/// <summary>
/// Faithful replacement for LochNessBuilder's <c>Builder&lt;T&gt;.New</c>.
/// <para>
/// Returns an AutoFixture composer with auto-properties omitted, so that ONLY the properties
/// explicitly configured via <c>.With(...)</c> are set; every other property is left at its
/// type-default (<c>null</c>/<c>0</c>/<c>false</c>) - exactly as LochNessBuilder behaved.
/// </para>
/// <para>
/// A fresh <see cref="Fixture"/> is created per call to avoid shared mutable state across
/// parallel test runs.
/// </para>
/// </summary>
public static class FixtureBuilder
{
    public static IPostprocessComposer<T> For<T>() => new Fixture().Build<T>().OmitAutoProperties();
}
