using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client.Models
{
    /// <summary>
    /// A data type for storing a T at each of the type positions in a single Locus.
    /// </summary>
    /// <typeparam name="T">The data that is stored at each type position.</typeparam>
    public class SingleLocusDetails<T>
    {
        public T One { get; set; }
        public T Two { get; set; }
    }
}
