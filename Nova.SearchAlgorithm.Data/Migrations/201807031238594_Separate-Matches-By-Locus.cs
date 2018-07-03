namespace Nova.SearchAlgorithm.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeparateMatchesByLocus : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.MatchingHlas", newName: "MatchingHlaAtA");
            CreateTable(
                "dbo.MatchingHlaAtB",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        TypePosition = c.Int(nullable: false),
                        PGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PGroup_Id);
            
            CreateTable(
                "dbo.MatchingHlaAtC",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        TypePosition = c.Int(nullable: false),
                        PGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PGroup_Id);
            
            CreateTable(
                "dbo.MatchingHlaAtDQB1",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        TypePosition = c.Int(nullable: false),
                        PGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PGroup_Id);
            
            CreateTable(
                "dbo.MatchingHlaAtDRB1",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        TypePosition = c.Int(nullable: false),
                        PGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PGroup_Id);
            
            DropColumn("dbo.MatchingHlaAtA", "LocusCode");
        }
        
        public override void Down()
        {
            AddColumn("dbo.MatchingHlaAtA", "LocusCode", c => c.Int(nullable: false));
            DropIndex("dbo.MatchingHlaAtDRB1", new[] { "PGroup_Id" });
            DropIndex("dbo.MatchingHlaAtDQB1", new[] { "PGroup_Id" });
            DropIndex("dbo.MatchingHlaAtC", new[] { "PGroup_Id" });
            DropIndex("dbo.MatchingHlaAtB", new[] { "PGroup_Id" });
            DropTable("dbo.MatchingHlaAtDRB1");
            DropTable("dbo.MatchingHlaAtDQB1");
            DropTable("dbo.MatchingHlaAtC");
            DropTable("dbo.MatchingHlaAtB");
            RenameTable(name: "dbo.MatchingHlaAtA", newName: "MatchingHlas");
        }
    }
}
