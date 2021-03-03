using System;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    /// <summary>
    /// In a few places we separate frequencies by resolution, at P group, G group, and g-group resolutions;
    /// as each needs to be processed slightly differently.
    ///
    /// This class is a wrapper of such a pair of data for convenience.
    /// </summary>
    internal class DataByResolution<T>
    {
        public T PGroup { get; set; }
        public T GGroup { get; set; }
        
        public T SmallGGroup { get; set; }

        public T GetByCategory(HaplotypeTypingCategory haplotypeTypingCategory)
        {
            return haplotypeTypingCategory switch
            {
                HaplotypeTypingCategory.GGroup => GGroup,
                HaplotypeTypingCategory.PGroup => PGroup,
                HaplotypeTypingCategory.SmallGGroup => SmallGGroup,
                _ => throw new ArgumentOutOfRangeException(nameof(haplotypeTypingCategory))
            };
        }

        public DataByResolution<TResult> Map<TResult>(Func<T, TResult> mapping)
        {
            return Map((_, value) => mapping(value));
        }

        public DataByResolution<TResult> Map<TResult>(Func<HaplotypeTypingCategory, T, TResult> mapping)
        {
            return MapAsync((category, value) => Task.FromResult(mapping(category, value))).Result;
        }

        public async Task<DataByResolution<TResult>> MapAsync<TResult>(Func<HaplotypeTypingCategory, T, Task<TResult>> mapping)
        {
            return new DataByResolution<TResult>
            {
                GGroup = await mapping(HaplotypeTypingCategory.GGroup, GGroup),
                PGroup = await mapping(HaplotypeTypingCategory.PGroup, PGroup),
                SmallGGroup = await mapping(HaplotypeTypingCategory.SmallGGroup, SmallGGroup)
            };
        }
    }
}