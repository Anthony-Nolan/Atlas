using System;
using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.MatchPrediction.Data.Models
{
    /// <summary>
    /// Contains only resolutions supported for storing HF sets. Should be a subset of <see cref="HlaTypingCategory"/> 
    /// </summary>
    public enum HaplotypeTypingCategory
    {
        GGroup,
        PGroup,
        SmallGGroup
    }

    public static class Converters
    {
        public static HlaTypingCategory ToHlaTypingCategory(this HaplotypeTypingCategory category)
        {
            return category switch
            {
                HaplotypeTypingCategory.GGroup => HlaTypingCategory.GGroup,
                HaplotypeTypingCategory.PGroup => HlaTypingCategory.PGroup,
                HaplotypeTypingCategory.SmallGGroup => HlaTypingCategory.SmallGGroup,
                _ => throw new ArgumentException(nameof(category))
            };
        }
    }
}