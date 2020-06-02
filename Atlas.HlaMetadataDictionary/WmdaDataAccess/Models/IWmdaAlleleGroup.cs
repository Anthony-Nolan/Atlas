using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.WmdaDataAccess.Models
{
    /// <summary>
    /// WMDA data types that group alleles by a shared property,
    /// such as G group and P group.
    /// </summary>
    internal interface IWmdaAlleleGroup: IWmdaHlaTyping
    {
        IEnumerable<string> Alleles { get; set; }
    }
}
