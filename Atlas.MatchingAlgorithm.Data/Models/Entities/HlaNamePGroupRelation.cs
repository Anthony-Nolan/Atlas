using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    public class HlaNamePGroupRelation
    {
        public long Id { get; set; }
        
        [ForeignKey("HlaName_Id")]
        public HlaName HlaName { get; set; }
        
        [ForeignKey("PGroup_Id")]
        public PGroupName PGroup { get; set; }
    }
}