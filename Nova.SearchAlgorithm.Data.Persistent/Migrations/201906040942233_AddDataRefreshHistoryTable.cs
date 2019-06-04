namespace Nova.SearchAlgorithm.Data.Persistent.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDataRefreshHistoryTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DataRefreshHistory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RefreshBeginUtc = c.DateTime(nullable: false),
                        RefreshEndUtc = c.DateTime(),
                        Database = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.RefreshEndUtc);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.DataRefreshHistory", new[] { "RefreshEndUtc" });
            DropTable("dbo.DataRefreshHistory");
        }
    }
}
