using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    /// <summary>
    /// WMDA data types that group alleles by a shared property,
    /// such as G group and P group.
    /// </summary>
    public interface IWmdaAlleleGroup: IWmdaHlaTyping
    {
        IEnumerable<string> Alleles { get; set; }
    }
}
