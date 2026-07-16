using System;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public static class HaplotypeInternerExtensions
{
    public static HaplotypeKey RemoveLoci(this HaplotypeKey key, Locus[] loci)
    {
        foreach (var locus in loci)
        {
            switch (locus)
            {
                case Locus.A:
                    key.A = 0;
                    break;
                case Locus.B:
                    key.B = 0;
                    break;
                case Locus.C:
                    key.C = 0;
                    break;
                case Locus.Dqb1:
                    key.Dqb1 = 0;
                    break;
                case Locus.Drb1:
                    key.Drb1 = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return key;
    }
}