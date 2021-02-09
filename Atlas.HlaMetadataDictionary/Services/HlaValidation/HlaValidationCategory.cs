using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.HlaMetadataDictionary.Services.HlaValidation
{
    /// <summary>
    /// Available target HLA categories for HLA validation.
    /// <para>Note: this has a different usage and intent to both the <see cref="HlaTypingCategory"/> enum, and the <see cref="TargetHlaCategory"/>, despite the overlap in values.</para>
    /// </summary>
    public enum HlaValidationCategory
    {
        GGroup,
        SmallGGroup
    }
}