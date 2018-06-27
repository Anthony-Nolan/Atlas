namespace Nova.SearchAlgorithm.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Donors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        DonorType = c.Int(nullable: false),
                        RegistryCode = c.Int(nullable: false),
                        A_1 = c.String(),
                        A_2 = c.String(),
                        B_1 = c.String(),
                        B_2 = c.String(),
                        C_1 = c.String(),
                        C_2 = c.String(),
                        DPB1_1 = c.String(),
                        DPB1_2 = c.String(),
                        DQB1_1 = c.String(),
                        DQB1_2 = c.String(),
                        DRB1_1 = c.String(),
                        DRB1_2 = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MatchingHlas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DonorId = c.Int(nullable: false),
                        TypePosition = c.Int(nullable: false),
                        LocusCode = c.Int(nullable: false),
                        PGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PGroupNames", t => t.PGroup_Id)
                .Index(t => t.PGroup_Id);
            
            CreateTable(
                "dbo.PGroupNames",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MatchingHlas", "PGroup_Id", "dbo.PGroupNames");
            DropIndex("dbo.MatchingHlas", new[] { "PGroup_Id" });
            DropTable("dbo.PGroupNames");
            DropTable("dbo.MatchingHlas");
            DropTable("dbo.Donors");
        }
    }
}
