using System;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public static class HaplotypeInternerExtensions
{
    public static bool EqualsAtLoci(this HaplotypeKey left, HaplotypeKey right, ISet<Locus> loci)
    {
        // Left nor right can be null
        if (left == right)
        {
            return true;
        }

        // If any of the loci are not present in the set, they are considered equal
        return (!loci.Contains(Locus.A) || left.A == right.A)
            && (!loci.Contains(Locus.B) || left.B == right.B)
            && (!loci.Contains(Locus.C) || left.C == right.C)
            && (!loci.Contains(Locus.Dqb1) || left.Dqb1 == right.Dqb1)
            && (!loci.Contains(Locus.Drb1) || left.Drb1 == right.Drb1);
    }

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