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
    public class FiveLociDetails<T>
    {
        public T A { get; set; }
        public T B { get; set; }
        public T C { get; set; }
        public T DQB1 { get; set; }
        public T DRB1 { get; set; }

        public FiveLociDetails<R> Map<R>(Func<string, T, R> mapping)
        {
            return new FiveLociDetails<R>
            {
                A = mapping("A", A),
                B = mapping("B", B),
                C = mapping("C", C),
                DQB1 = mapping("DQB1", DQB1),
                DRB1 = mapping("DRB1", DRB1),
            };
        }

        public void Each(Action<string, T> action)
        {
            action("A", A);
            action("B", B);
            action("C", C);
            action("DQB1", DQB1);
            action("DRB1", DRB1);
        }
    }
}
