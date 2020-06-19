using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }

        /// <summary>
        /// There are times where the value held in <see cref="HlaTyping"/> HLA typing is not the one
        /// that should be used in HMD pre-calculations.
        /// E.g., Where an allele or serology has been deemed "identical to" a second typing within the same HLA nomenclature version.
        /// This property is therefore used to hold the typing name that should actually be used in HMD pre-calculations.
        /// </summary>
        HlaTyping TypingUsedInMatching { get; }
    }
}
