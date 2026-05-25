using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.SearchTracking.Data.Models;

/// <summary>
/// One row per donor result location received from the ACA Worker across all parallel MPA batches.
/// Primary key is the composite (MetadataId, DonorId) as configured in <see cref="Context.SearchTrackingContext"/>.
/// </summary>
public class SearchRequestParallelMatchPredictionResultLocation
{

    /// <summary>Foreign key to <see cref="SearchRequestParallelMatchPredictionMetadata"/></summary>
    [ForeignKey(nameof(Metadata))]
    public int MetadataId { get; set; }

    public SearchRequestParallelMatchPredictionMetadata Metadata { get; set; }

    public int DonorId { get; set; }

    /// <summary>Blob filename (relative to the match-prediction-results container) for this donor's MPA result.</summary>
    [Required]
    public string ResultBlobFileName { get; set; }
}