using System;

namespace Atlas.Utils.Core.Models
{
    /// <summary>
    /// Possible statuses representing test results for a subject for a given virus type.
    /// A common virus tested for is CMV - but this status can also be applied to other viruses.
    ///
    /// Positive = tested positive for the virus
    /// Negative = tested negative for the virus
    /// Equivocal = tests completed, yielding inconclusive results
    /// Unknown = tests have not been completed - this could also be represented by a null value
    /// </summary>
    public enum VirologyStatus
    {
        Positive,
        Negative,
        Equivocal,
        Unknown
    }

    [Obsolete("Use VirologyStatus")]
    public enum CmvAntibodyType
    {
        Positive = VirologyStatus.Positive,
        Negative = VirologyStatus.Negative,
        Equivocal = VirologyStatus.Equivocal,
        Unknown = VirologyStatus.Unknown
    }
}