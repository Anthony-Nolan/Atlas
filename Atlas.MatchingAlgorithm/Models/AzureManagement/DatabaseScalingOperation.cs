using System;

namespace Atlas.MatchingAlgorithm.Models.AzureManagement;

/// <summary>A scale operation that Azure accepted, and that can be waited for.</summary>
/// <param name="StartTime">The start time that Azure gave the operation. Null when running locally: nothing was sent.</param>
public record DatabaseScalingOperation(string DatabaseName, AzureDatabaseSize DatabaseSize, DateTime? StartTime);
