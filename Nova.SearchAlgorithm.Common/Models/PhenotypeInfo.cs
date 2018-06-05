using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Common.Models
{
    /// <summary>
    /// Data type to hold one instance of T for each of the five HLA loci and each type position within.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each loci position.</typeparam>
    public class PhenotypeInfo<T>
    {
        public T A_1 { get; set; }
        public T A_2 { get; set; }
        public T B_1 { get; set; }
        public T B_2 { get; set; }
        public T C_1 { get; set; }
        public T C_2 { get; set; }
        public T DQB1_1 { get; set; }
        public T DQB1_2 { get; set; }
        public T DRB1_1 { get; set; }
        public T DRB1_2 { get; set; }

        public PhenotypeInfo<R> Map<R>(Func<Locus, TypePositions, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A_1 = mapping(Locus.A, TypePositions.One, A_1),
                A_2 = mapping(Locus.A, TypePositions.Two, A_1),
                B_1 = mapping(Locus.B, TypePositions.One, B_1),
                B_2 = mapping(Locus.B, TypePositions.Two, B_2),
                C_1 = mapping(Locus.C, TypePositions.One, C_1),
                C_2 = mapping(Locus.C, TypePositions.Two, C_2),
                DQB1_1 = mapping(Locus.Dqb1, TypePositions.One, DQB1_1),
                DQB1_2 = mapping(Locus.Dqb1, TypePositions.Two, DQB1_2),
                DRB1_1 = mapping(Locus.Drb1, TypePositions.One, DRB1_1),
                DRB1_2 = mapping(Locus.Drb1, TypePositions.Two, DRB1_2),
            };
        }

        public void EachPosition(Action<Locus, TypePositions, T> action)
        {
            action(Locus.A, TypePositions.One, A_1);
            action(Locus.A, TypePositions.Two, A_2);
            action(Locus.B, TypePositions.One, B_1);
            action(Locus.B, TypePositions.Two, B_2);
            action(Locus.C, TypePositions.One, C_1);
            action(Locus.C, TypePositions.Two, C_2);
            action(Locus.Dqb1, TypePositions.One, DQB1_1);
            action(Locus.Dqb1, TypePositions.Two, DQB1_2);
            action(Locus.Drb1, TypePositions.One, DRB1_1);
            action(Locus.Drb1, TypePositions.Two, DRB1_2);
        }

        public void EachLocus(Action<Locus, T, T> action)
        {
            action(Locus.A, A_1, A_2);
            action(Locus.B, B_1, B_2);
            action(Locus.C, C_1, C_2);
            action(Locus.Dqb1, DQB1_1, DQB1_2);
            action(Locus.Drb1, DRB1_1, DRB1_2);
        }

        public async Task WhenAllLoci(Func<Locus, T, T, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A_1, A_2),
                action(Locus.B, B_1, B_2),
                action(Locus.C, C_1, C_2),
                action(Locus.Dqb1, DQB1_1, DQB1_2),
                action(Locus.Drb1, DRB1_1, DRB1_2)); 
        }

        public async Task<PhenotypeInfo<R>> WhenAllPositions<R>(Func<Locus, TypePositions, T, Task<R>> action)
        {
            R[] results = await Task.WhenAll(
                action(Locus.A, TypePositions.One, A_1),
                action(Locus.A, TypePositions.Two, A_2),
                action(Locus.B, TypePositions.One, B_1),
                action(Locus.B, TypePositions.Two, B_2),
                action(Locus.C, TypePositions.One, C_1),
                action(Locus.C, TypePositions.Two, C_2),
                action(Locus.Dqb1, TypePositions.One, DQB1_1),
                action(Locus.Dqb1, TypePositions.Two, DQB1_2),
                action(Locus.Drb1, TypePositions.One, DRB1_1),
                action(Locus.Drb1, TypePositions.Two, DRB1_2));

            return new PhenotypeInfo<R>
            {
                A_1 = results[0],
                A_2 = results[1],
                B_1 = results[2],
                B_2 = results[3],
                C_1 = results[4],
                C_2 = results[5],
                DQB1_1 = results[6],
                DQB1_2 = results[7],
                DRB1_1 = results[8],
                DRB1_2 = results[9]
            };
        }
    }
}
