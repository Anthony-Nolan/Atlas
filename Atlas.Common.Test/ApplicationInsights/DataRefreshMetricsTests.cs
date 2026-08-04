using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Atlas.Common.ApplicationInsights;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.ApplicationInsights;

/// <summary>
/// Guards the one runtime invariant of the Data Refresh telemetry contract that nothing else can catch until a
/// fifteen-hour run is already under way: App Insights caches one aggregator per (metric name + ordered dimension
/// names) and THROWS if the same metric name later arrives with a different dimension-key set.
/// </summary>
[TestFixture]
public class DataRefreshMetricsTests
{
    [Test]
    public void Dims_ForAnyOperationAndLocus_AlwaysSuppliesTheSameDimensionKeys()
    {
        var withLocus = DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DbBulkInsert, "A");
        var withoutLocus = DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorStreamRead);

        withLocus.Keys.Should().BeEquivalentTo(withoutLocus.Keys);
        withLocus.Keys.Should().BeEquivalentTo(new[] { DataRefreshMetrics.OperationDimension, DataRefreshMetrics.LocusDimension });
    }

    [Test]
    public void Dims_WhenLocusIsNotSupplied_DefaultsToAll()
    {
        var dims = DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorValidation);

        dims[DataRefreshMetrics.LocusDimension].Should().Be(DataRefreshMetrics.Locus_All);
    }

    [Test]
    public void StageDims_DoesNotShareADimensionKeySetWithDims()
    {
        var stageDims = DataRefreshMetrics.StageDims("DonorImport");

        stageDims.Keys.Should().BeEquivalentTo(new[] { DataRefreshMetrics.StageDimension });
        stageDims.Keys.Should().NotBeEquivalentTo(DataRefreshMetrics.Dims("any").Keys);
    }

    [Test]
    public void RuntimeDims_DoesNotShareADimensionKeySetWithDims()
    {
        var runtimeDims = DataRefreshMetrics.RuntimeDims(DataRefreshMetrics.Counter_CpuPercent);

        runtimeDims.Keys.Should().BeEquivalentTo(new[] { DataRefreshMetrics.CounterDimension });
        runtimeDims.Keys.Should().NotBeEquivalentTo(DataRefreshMetrics.Dims("any").Keys);
    }

    [Test]
    public void MetricNames_AreAllDistinct()
    {
        var metricNames = ConstantsWithPrefix("").Where(c => c.Value.StartsWith("DataRefresh.")).Select(c => c.Value).ToList();

        metricNames.Should().OnlyHaveUniqueItems();
        metricNames.Should().HaveCountGreaterThan(1);
    }

    /// <summary>
    /// A duplicated dimension value would silently merge two unrelated measurements into one series - the sort of
    /// copy-paste slip that is invisible until the analysis says something impossible.
    /// </summary>
    [Test]
    public void OperationDimensionValues_AreAllDistinct()
    {
        ConstantsWithPrefix("Operation_").Select(c => c.Value).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void CounterDimensionValues_AreAllDistinct()
    {
        ConstantsWithPrefix("Counter_").Select(c => c.Value).Should().OnlyHaveUniqueItems();
    }

    private static List<(string Name, string Value)> ConstantsWithPrefix(string prefix) =>
        typeof(DataRefreshMetrics)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(f => (f.Name, (string)f.GetRawConstantValue()))
            .ToList();
}
