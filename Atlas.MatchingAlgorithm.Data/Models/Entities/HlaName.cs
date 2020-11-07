using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable ClassNeverInstantiated.Global

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    [Table("HlaNames")]
    public class HlaName
    {
        public int Id { get; set; }
        
        /// <summary>
        /// String representation of HLA, as stored in the <see cref="Donor"/> table.
        /// </summary>
        public string Name { get; set; }
    }
}