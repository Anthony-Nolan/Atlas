using System.ComponentModel.DataAnnotations.Schema;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Exceptions;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities.MatchingHla
{
    public abstract class MatchingHla
    {
        public long Id { get; set; }
        public int DonorId { get; set; }
        public int TypePosition { get; set; }
        [ForeignKey("PGroup_Id")]
        public PGroupName PGroup { get; set; }

        public static MatchingHla EmptyMatchingEntityForLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return new MatchingHlaAtA();
                case Locus.B:
                    return new MatchingHlaAtB();
                case Locus.C:
                    return new MatchingHlaAtC();
                case Locus.Dqb1:
                    return new MatchingHlaAtDqb1();
                case Locus.Drb1:
                    return new MatchingHlaAtDrb1();
                default:
                    throw new DataHttpException($"Could not instantiate MatchingHla entity for unknown locus {locus}");
            }
        }
    }
}