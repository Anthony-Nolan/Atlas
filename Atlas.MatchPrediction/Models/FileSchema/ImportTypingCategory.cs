using System;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Models.FileSchema
{
    public enum ImportTypingCategory
    {
        /// <summary>
        /// Three field "G-group" typing resolution, e.g. 01:01:01G
        /// </summary>
        LargeGGroup,

        /// <summary>
        /// Two field "g-group" typing resolution, e.g. 01:01g
        /// </summary>
        SmallGGroup
    }

    internal static class Converters
    {
        internal static HlaTypingCategory ToInternalTypingCategory(this ImportTypingCategory importTypingCategory) => importTypingCategory switch
        {
            ImportTypingCategory.LargeGGroup => HlaTypingCategory.GGroup,
            ImportTypingCategory.SmallGGroup => HlaTypingCategory.SmallGGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(importTypingCategory))
        };

        internal static HaplotypeTypingCategory ToDatabaseTypingCategory(this ImportTypingCategory importTypingCategory) =>
            importTypingCategory switch
            {
                ImportTypingCategory.LargeGGroup => HaplotypeTypingCategory.GGroup,
                ImportTypingCategory.SmallGGroup => HaplotypeTypingCategory.SmallGGroup,
                _ => throw new ArgumentOutOfRangeException(nameof(importTypingCategory))
            };

        internal static HlaValidationCategory ToHlaValidationCategory(this ImportTypingCategory importTypingCategory) => importTypingCategory switch
        {
            ImportTypingCategory.LargeGGroup => HlaValidationCategory.GGroup,
            ImportTypingCategory.SmallGGroup => HlaValidationCategory.SmallGGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(importTypingCategory))
        };
    }
}