namespace Nova.SearchAlgorithm.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MatchesTableIdToBigInt : DbMigration
    {
        public override void Up()
        {
//            DropPrimaryKey("dbo.MatchingHlaAtA");
//            DropPrimaryKey("dbo.MatchingHlaAtB");
//            DropPrimaryKey("dbo.MatchingHlaAtC");
//            DropPrimaryKey("dbo.MatchingHlaAtDQB1");
//            DropPrimaryKey("dbo.MatchingHlaAtDRB1");
//            AlterColumn("dbo.MatchingHlaAtA", "Id", c => c.Long(nullable: false, identity: true));
//            AlterColumn("dbo.MatchingHlaAtB", "Id", c => c.Long(nullable: false, identity: true));
//            AlterColumn("dbo.MatchingHlaAtC", "Id", c => c.Long(nullable: false, identity: true));
//            AlterColumn("dbo.MatchingHlaAtDQB1", "Id", c => c.Long(nullable: false, identity: true));
//            AlterColumn("dbo.MatchingHlaAtDRB1", "Id", c => c.Long(nullable: false, identity: true));
//            AddPrimaryKey("dbo.MatchingHlaAtA", "Id");
//            AddPrimaryKey("dbo.MatchingHlaAtB", "Id");
//            AddPrimaryKey("dbo.MatchingHlaAtC", "Id");
//            AddPrimaryKey("dbo.MatchingHlaAtDQB1", "Id");
//            AddPrimaryKey("dbo.MatchingHlaAtDRB1", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.MatchingHlaAtDRB1");
            DropPrimaryKey("dbo.MatchingHlaAtDQB1");
            DropPrimaryKey("dbo.MatchingHlaAtC");
            DropPrimaryKey("dbo.MatchingHlaAtB");
            DropPrimaryKey("dbo.MatchingHlaAtA");
            AlterColumn("dbo.MatchingHlaAtDRB1", "Id", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.MatchingHlaAtDQB1", "Id", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.MatchingHlaAtC", "Id", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.MatchingHlaAtB", "Id", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.MatchingHlaAtA", "Id", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.MatchingHlaAtDRB1", "Id");
            AddPrimaryKey("dbo.MatchingHlaAtDQB1", "Id");
            AddPrimaryKey("dbo.MatchingHlaAtC", "Id");
            AddPrimaryKey("dbo.MatchingHlaAtB", "Id");
            AddPrimaryKey("dbo.MatchingHlaAtA", "Id");
        }
    }
}
