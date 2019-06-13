namespace Nova.SearchAlgorithm.Data.Persistent.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialDatabaseSplit : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ConfidenceWeightings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                        Weight = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.GradeWeightings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                        Weight = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.GradeWeightings", new[] { "Name" });
            DropIndex("dbo.ConfidenceWeightings", new[] { "Name" });
            DropTable("dbo.GradeWeightings");
            DropTable("dbo.ConfidenceWeightings");
        }
    }
}
