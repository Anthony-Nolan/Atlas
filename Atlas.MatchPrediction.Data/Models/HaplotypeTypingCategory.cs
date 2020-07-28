using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.MatchPrediction.Data.Models
{
    /// <summary>
    /// Contains only resolutions supported for storing HF sets. Should be a subset of <see cref="HlaTypingCategory"/> 
    /// </summary>
    public enum HaplotypeTypingCategory
    {
        GGroup,
        PGroup
    }
}