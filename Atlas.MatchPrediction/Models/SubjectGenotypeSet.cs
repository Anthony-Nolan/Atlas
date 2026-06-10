using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models;

public class SubjectGenotypeSet
{
    public bool IsUnrepresented { get; }
    public ICollection<GenotypeAtDesiredResolutions> Genotypes { get; }
    public decimal SumOfLikelihoods { get; }

    public SubjectGenotypeSet(bool isUnrepresented, ICollection<GenotypeAtDesiredResolutions> genotypes, decimal sumOfLikelihoods)
    {
        IsUnrepresented = isUnrepresented;
        Genotypes = genotypes;
        SumOfLikelihoods = sumOfLikelihoods;
    }
}