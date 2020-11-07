using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    [Table("PGroupNames")]
    public class PGroupName
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}