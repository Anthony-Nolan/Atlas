using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>One subject of the request: the typing, or one donor.</summary>
internal class ComputedSubject
{
    /// <summary>The code of the donor; not given for a typing.</summary>
    public string ExternalDonorCode { get; set; }

    public int? DonorId { get; set; }

    /// <summary>False when no donor has the code.</summary>
    public bool Found { get; set; }

    public bool? IsAvailableForSearch { get; set; }

    public string RegistryCode { get; set; }

    public string EthnicityCode { get; set; }

    /// <summary>The typing that was computed: the typing of the request, or the donor's typing in the database.</summary>
    public string HlaTyping { get; set; }

    /// <summary>
    /// For a donor of a request that saves: true when its link rows were written. They are written only when all its
    /// requested values are stored.
    /// </summary>
    public bool? LinkRowsWritten { get; set; }

    /// <summary>Why the subject could not be computed or saved.</summary>
    public string Error { get; set; }

    public List<ComputedGenotypeSet> GenotypeSets { get; set; }

    public static ComputedSubject From(GenotypeSetDebugSubject subject) => new()
    {
        ExternalDonorCode = subject.ExternalDonorCode,
        DonorId = subject.DonorId,
        Found = subject.Found,
        IsAvailableForSearch = subject.IsAvailableForSearch,
        RegistryCode = subject.RegistryCode,
        EthnicityCode = subject.EthnicityCode,
        HlaTyping = subject.HlaTyping?.PrettyPrint(),
        LinkRowsWritten = subject.LinkRowsWritten,
        Error = subject.Error,
        GenotypeSets = subject.GenotypeSets.Select(ComputedGenotypeSet.From).ToList()
    };
}
