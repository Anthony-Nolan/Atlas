using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    public class HlaNamePGroupRelation
    {
        public long Id { get; set; }
        
        public int HlaName_Id { get; set; }
        
        [ForeignKey(nameof(HlaName_Id))]
        public HlaName HlaName { get; set; }

        public int PGroup_Id { get; set; }

        [ForeignKey(nameof(PGroup_Id))]
        public PGroupName PGroup { get; set; }
    }
    
    [Table("HlaNamePGroupRelationAtA")]
    public class HlaNamePGroupRelationAtA : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtB")]
    public class HlaNamePGroupRelationAtB : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtC")]
    public class HlaNamePGroupRelationAtC : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDQB1")]
    public class HlaNamePGroupRelationAtDqb1 : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDRB1")]
    public class HlaNamePGroupRelationAtDrb1 : HlaNamePGroupRelation
    {
    }
}