namespace Nova.SearchAlgorithm.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePersistentData : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ConfidenceWeightings", new[] { "Name" });
            DropIndex("dbo.GradeWeightings", new[] { "Name" });
            DropTable("dbo.ConfidenceWeightings");
            DropTable("dbo.GradeWeightings");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.GradeWeightings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                        Weight = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ConfidenceWeightings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                        Weight = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.GradeWeightings", "Name", unique: true);
            CreateIndex("dbo.ConfidenceWeightings", "Name", unique: true);
        }
    }
}
