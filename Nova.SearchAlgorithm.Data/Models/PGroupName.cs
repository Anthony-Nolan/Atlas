using System.ComponentModel.DataAnnotations.Schema;

namespace Nova.SearchAlgorithm.Data.Models
{
    [Table("PGroupNames")]
    public class PGroupName
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}