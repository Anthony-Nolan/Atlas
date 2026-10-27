using System;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// The rows that a load read from the database for a content do not have its content hash. The load of the shared entry
/// throws it, so that no caller gets these rows as the content: see <see cref="HaplotypeFrequencyCache"/>.
/// </summary>
internal sealed class FrequencySetContentMismatchException(int setId)
    : Exception($"The rows of HF set {setId} do not have the content hash in its row.")
{
    public int SetId { get; } = setId;
}
