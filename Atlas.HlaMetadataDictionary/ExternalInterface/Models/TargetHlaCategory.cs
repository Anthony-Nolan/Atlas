using System;
using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models
{
    /// <summary>
    /// Available target HLA categories for the HLA converter.
    /// <para>Note: this has a different usage and intent to the <see cref="HlaTypingCategory"/> enum,
    /// despite the overlap in values.</para>
    /// </summary>
    public enum TargetHlaCategory
    {
        TwoFieldAlleleIncludingExpressionSuffix,
        TwoFieldAlleleExcludingExpressionSuffix,
        GGroup,
        SmallGGroup,
        PGroup,
        Serology
    }

    public static class Converters
    {
        public static TargetHlaCategory ToTargetHlaCategory(this HlaTypingCategory hlaTypingCategory)
        {
            // TODO: ATLAS-838 - add case for small g group
            return hlaTypingCategory switch
            {
                HlaTypingCategory.Allele => throw new NotSupportedException(),
                HlaTypingCategory.GGroup => TargetHlaCategory.GGroup,
                HlaTypingCategory.PGroup => TargetHlaCategory.PGroup,
                HlaTypingCategory.AlleleStringOfNames => throw new NotSupportedException(),
                HlaTypingCategory.AlleleStringOfSubtypes => throw new NotSupportedException(),
                HlaTypingCategory.NmdpCode => throw new NotSupportedException(),
                HlaTypingCategory.XxCode => throw new NotSupportedException(),
                HlaTypingCategory.Serology => TargetHlaCategory.Serology,
                _ => throw new ArgumentException(nameof(hlaTypingCategory))
            };
        }
    }
}