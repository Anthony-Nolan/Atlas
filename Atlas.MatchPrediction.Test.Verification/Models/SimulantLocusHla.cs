using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class SimulantLocusHla
    {
        public Locus Locus { get; set; }
        public LocusInfo<string> HlaTyping { get; set; }
        public int GenotypeSimulantId { get; set; }
    }

    internal static class SimulantLocusHlaExtensions
    {
        public static SimulatedHlaTyping ToSimulatedHlaTyping(this IEnumerable<SimulantLocusHla> locusHlaTypings)
        {
            locusHlaTypings = locusHlaTypings.ToList();

            return new SimulatedHlaTyping
            {
                A_1 = locusHlaTypings.Single(hla => hla.Locus == Locus.A).HlaTyping.Position1,
                A_2 = locusHlaTypings.Single(hla => hla.Locus == Locus.A).HlaTyping.Position2,
                B_1 = locusHlaTypings.Single(hla => hla.Locus == Locus.B).HlaTyping.Position1,
                B_2 = locusHlaTypings.Single(hla => hla.Locus == Locus.B).HlaTyping.Position2,
                C_1 = locusHlaTypings.Single(hla => hla.Locus == Locus.C).HlaTyping.Position1,
                C_2 = locusHlaTypings.Single(hla => hla.Locus == Locus.C).HlaTyping.Position2,
                Dqb1_1 = locusHlaTypings.Single(hla => hla.Locus == Locus.Dqb1).HlaTyping.Position1,
                Dqb1_2 = locusHlaTypings.Single(hla => hla.Locus == Locus.Dqb1).HlaTyping.Position2,
                Drb1_1 = locusHlaTypings.Single(hla => hla.Locus == Locus.Drb1).HlaTyping.Position1,
                Drb1_2 = locusHlaTypings.Single(hla => hla.Locus == Locus.Drb1).HlaTyping.Position2
            };
        }
    }
}
