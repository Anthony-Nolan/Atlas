using System;
using Nova.Utils.Models;
using Locus = Atlas.MatchingAlgorithm.Common.Models.Locus;

namespace Atlas.MatchingAlgorithm.Helpers
{
    public static class LocusConverter
    {
        public static Locus? ToAlgorithmLocus(this LocusType utilsLocus)
        {
            switch (utilsLocus)
            {
                case LocusType.A:
                    return Locus.A;
                case LocusType.B:
                    return Locus.B;
                case LocusType.C:
                    return Locus.C;
                case LocusType.Drb1:
                    return Locus.Drb1;
                case LocusType.Dqb1:
                    return Locus.Dqb1;
                case LocusType.Dpb1:
                    return Locus.Dpb1;
                case LocusType.Drb3:
                case LocusType.Drb4:
                case LocusType.Drb5:
                case LocusType.Dqa1:
                case LocusType.Dpa1:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(utilsLocus), utilsLocus, null);
            }
        }
    }
}