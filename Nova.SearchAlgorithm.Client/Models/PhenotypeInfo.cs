using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client.Models
{
    // TODO:NOVA-929 rename to 'phenotype' or similar?
    // TODO:NOVA-929 should this be combined with SingleLocusDetail
    /// <summary>
    /// Data type to hold one instance of T for each of the five HLA loci.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each loci.</typeparam>
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

        public PhenotypeInfo<R> Map<R>(Func<string, TypePositions, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A_1 = mapping("A", TypePositions.One, A_1),
                A_2 = mapping("A", TypePositions.Two, A_1),
                B_1 = mapping("B", TypePositions.One, B_1),
                B_2 = mapping("B", TypePositions.Two, B_2),
                C_1 = mapping("C", TypePositions.One, C_1),
                C_2 = mapping("C", TypePositions.Two, C_2),
                DQB1_1 = mapping("DQB1", TypePositions.One, DQB1_1),
                DQB1_2 = mapping("DQB1", TypePositions.Two, DQB1_2),
                DRB1_1 = mapping("DRB1", TypePositions.One, DRB1_1),
                DRB1_2 = mapping("DRB1", TypePositions.Two, DRB1_2),
            };
        }

        public void Each(Action<string, TypePositions, T> action)
        {
            action("A", TypePositions.One, A_1);
            action("A", TypePositions.Two, A_2);
            action("B", TypePositions.One, B_1);
            action("B", TypePositions.Two, B_2);
            action("C", TypePositions.One, C_1);
            action("C", TypePositions.Two, C_2);
            action("DQB1", TypePositions.One, DQB1_1);
            action("DQB1", TypePositions.Two, DQB1_2);
            action("DRB1", TypePositions.One, DRB1_1);
            action("DRB1", TypePositions.Two, DRB1_2);
        }
    }
}
