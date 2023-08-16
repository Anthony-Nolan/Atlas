using System;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Utils;

namespace Atlas.MatchPrediction.Models;

public class GenotypeAtDesiredResolutions
{
    /// <summary>
    /// HLA at the resolution at which they were stored.
    /// i.e. G group, or P group if any null alleles are present in the haplotype.
    /// </summary>
    public PhenotypeInfo<string> HaplotypeResolution { get; }

    /// <summary>
    /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
    /// i.e. P group, or G group for null expressing alleles. 
    /// </summary>
    public PhenotypeInfo<string> StringMatchableResolution { get; }

    /// <summary>
    /// Likelihood of this genotype.
    ///
    /// Stored with the genotype to avoid dictionary lookups when calculating final likelihoods, as looking up the same genotype multiple times
    /// for different patient/donor pairs is inefficient 
    /// </summary>
    public decimal GenotypeLikelihood { get; }

    private GenotypeAtDesiredResolutions(PhenotypeInfo<HlaAtKnownTypingCategory> haplotypeResolution, PhenotypeInfo<string> stringMatchableResolution, decimal genotypeLikelihood)
    {
        HaplotypeResolution = haplotypeResolution.ToHlaNames();
        StringMatchableResolution = stringMatchableResolution;
        GenotypeLikelihood = genotypeLikelihood;
    }

    public static async Task<GenotypeAtDesiredResolutions> FromHaplotypeResolutions(
        PhenotypeInfo<HlaAtKnownTypingCategory> haplotypeResolutions,
        IHlaMetadataDictionary hlaMetadataDictionary,
        decimal genotypeLikelihood)
    {
        var stringMatchableResolutions = (await haplotypeResolutions.MapAsync(async (locus, _, hla) =>
        {
            if (hla?.Hla == null)
            {
                return null;
            }

            return hla.TypingCategory switch
            {
                HaplotypeTypingCategory.GGroup => await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, hla.Hla),
                HaplotypeTypingCategory.PGroup => hla.Hla,
                HaplotypeTypingCategory.SmallGGroup => await hlaMetadataDictionary.ConvertSmallGGroupToPGroup(locus, hla.Hla),
                _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
            };
        })).CopyExpressingAllelesToNullPositions();

        return new GenotypeAtDesiredResolutions(haplotypeResolutions, stringMatchableResolutions, genotypeLikelihood);
    }
}