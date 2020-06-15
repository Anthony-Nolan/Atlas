namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models
{
    /// <summary>
    /// Available target HLA categories for the HLA converter.
    /// <para>Note: this has a different usage and intent to the <see cref="Common.GeneticData.Hla.Models.HlaTypingCategory"/> HlaTypingCategory enum,
    /// despite the overlap in values.</para>
    /// </summary>
    public enum TargetHlaCategory
    {
        TwoFieldAlleleIncludingExpressionSuffix,
        TwoFieldAlleleExcludingExpressionSuffix,
        GGroup,
        PGroup,
        Serology
    }
}