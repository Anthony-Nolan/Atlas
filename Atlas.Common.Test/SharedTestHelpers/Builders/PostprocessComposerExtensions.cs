using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AutoFixture;
using AutoFixture.Dsl;

namespace Atlas.Common.Test.SharedTestHelpers.Builders;

/// <summary>
/// Shims that let test code that previously used LochNessBuilder keep its call sites unchanged
/// after migrating to AutoFixture's <see cref="IPostprocessComposer{T}"/>.
/// </summary>
public static class PostprocessComposerExtensions
{
    /// <summary>Equivalent to LochNessBuilder's <c>.Build()</c> - creates a single instance.</summary>
    public static T Build<T>(this IPostprocessComposer<T> composer) => composer.Create();

    /// <summary>Equivalent to LochNessBuilder's <c>.Build(n)</c> - creates <paramref name="count"/> instances.</summary>
    public static IEnumerable<T> Build<T>(this IPostprocessComposer<T> composer, int count) => composer.CreateMany(count);

    /// <summary>
    /// Replaces LochNessBuilder's value-cycling overloads (<c>.With(p, v1, v2, ...)</c> /
    /// <c>.With(p, enumerable)</c>): each instance produced by <c>.Build(n)</c> takes the next value
    /// from <paramref name="values"/>, cycling back to the start when exhausted.
    /// </summary>
    public static IPostprocessComposer<T> WithSequence<T, TProp>(
        this IPostprocessComposer<T> composer,
        Expression<Func<T, TProp>> propertyPicker,
        IEnumerable<TProp> values)
    {
        var cycle = Cycle(values).GetEnumerator();
        return composer.With(propertyPicker, () =>
        {
            cycle.MoveNext();
            return cycle.Current;
        });
    }

    private static IEnumerable<TProp> Cycle<TProp>(IEnumerable<TProp> values)
    {
        // Re-enumerate (rather than buffer) so that infinite sequences such as
        // Enumerable.Range(1, int.MaxValue).Select(...) are consumed lazily and never materialised.
        while (true)
        {
            var any = false;
            foreach (var value in values)
            {
                any = true;
                yield return value;
            }

            if (!any)
            {
                yield break;
            }
        }
    }
}
