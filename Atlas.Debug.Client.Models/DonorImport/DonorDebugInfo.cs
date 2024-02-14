using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using System;

namespace Atlas.Debug.Client.Models.DonorImport;

/// <summary>
/// Info of a donor that was found in the target donor database during a debug request.
/// </summary>
public class DonorDebugInfo : IEquatable<DonorDebugInfo>
{
    /// <summary>
    /// Equivalent to recordId in the donor import file
    /// </summary>
    public string ExternalDonorCode { get; set; }

    public string DonorType { get; set; }
    public string RegistryCode { get; set; }
    public string EthnicityCode { get; set; }
    public PhenotypeInfoTransfer<string> Hla { get; set; }

    #region IEquatable implementation
    /// <inheritdoc />
    public bool Equals(DonorDebugInfo other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return
            ExternalDonorCode == other.ExternalDonorCode &&
            DonorType == other.DonorType &&
            RegistryCode == other.RegistryCode &&
            EthnicityCode == other.EthnicityCode &&
            // `PhenotypeInfoTransfer` does not implement IEquatable, but `PhenotypeInfo` does.
            Hla?.ToPhenotypeInfo() == other.Hla?.ToPhenotypeInfo();
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DonorDebugInfo)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(ExternalDonorCode, DonorType, RegistryCode, EthnicityCode, Hla?.ToPhenotypeInfo());
    }
    #endregion
}