namespace HospitalManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateDatabaseForAuthentication : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        LogID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        EventType = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false, maxLength: 500),
                        Timestamp = c.DateTime(nullable: false),
                        IPAddress = c.String(maxLength: 45),
                        AdditionalData = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.LogID)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AuditLogs", "UserID", "dbo.Users");
            DropIndex("dbo.AuditLogs", new[] { "UserID" });
            DropTable("dbo.AuditLogs");
        }
    }
}
