namespace HospitalManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Appointments",
                c => new
                    {
                        AppointmentID = c.Int(nullable: false, identity: true),
                        PatientID = c.Int(nullable: false),
                        StaffID = c.Int(nullable: false),
                        RoomID = c.Int(),
                        AppointmentDate = c.DateTime(nullable: false),
                        Duration = c.Int(nullable: false),
                        Type = c.String(nullable: false, maxLength: 50),
                        Status = c.String(maxLength: 20),
                        Reason = c.String(maxLength: 255),
                        Notes = c.String(maxLength: 1000),
                        CreatedDate = c.DateTime(nullable: false),
                        CreatedBy = c.Int(),
                    })
                .PrimaryKey(t => t.AppointmentID)
                .ForeignKey("dbo.Rooms", t => t.RoomID)
                .ForeignKey("dbo.Patients", t => t.PatientID, cascadeDelete: true)
                .ForeignKey("dbo.Staffs", t => t.StaffID, cascadeDelete: true)
                .Index(t => t.PatientID)
                .Index(t => t.StaffID)
                .Index(t => t.RoomID);
            
            CreateTable(
                "dbo.Patients",
                c => new
                    {
                        PatientID = c.Int(nullable: false, identity: true),
                        PatientCode = c.String(nullable: false, maxLength: 20),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        LastName = c.String(nullable: false, maxLength: 50),
                        DOB = c.DateTime(nullable: false),
                        Gender = c.String(maxLength: 1),
                        Phone = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        Address = c.String(maxLength: 255),
                        EmergencyContact = c.String(maxLength: 100),
                        EmergencyPhone = c.String(maxLength: 20),
                        InsuranceProvider = c.String(maxLength: 100),
                        InsuranceNumber = c.String(maxLength: 50),
                        BloodType = c.String(maxLength: 5),
                        Allergies = c.String(maxLength: 500),
                        MedicalConditions = c.String(maxLength: 500),
                        CreatedDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PatientID)
                .Index(t => t.PatientCode, unique: true);
            
            CreateTable(
                "dbo.MedicalRecords",
                c => new
                    {
                        RecordID = c.Int(nullable: false, identity: true),
                        PatientID = c.Int(nullable: false),
                        StaffID = c.Int(nullable: false),
                        VisitDate = c.DateTime(nullable: false),
                        ChiefComplaint = c.String(maxLength: 500),
                        Diagnosis = c.String(maxLength: 500),
                        Treatment = c.String(maxLength: 1000),
                        Prescription = c.String(maxLength: 1000),
                        VitalSigns = c.String(maxLength: 500),
                        Notes = c.String(maxLength: 2000),
                        FollowUpDate = c.DateTime(),
                        Status = c.String(maxLength: 20),
                        CreatedDate = c.DateTime(nullable: false),
                        UpdatedDate = c.DateTime(),
                        UpdatedBy = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.RecordID)
                .ForeignKey("dbo.Patients", t => t.PatientID, cascadeDelete: true)
                .ForeignKey("dbo.Staffs", t => t.StaffID, cascadeDelete: true)
                .Index(t => t.PatientID)
                .Index(t => t.StaffID);
            
            CreateTable(
                "dbo.Staffs",
                c => new
                    {
                        StaffID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        DepartmentID = c.Int(),
                        EmployeeCode = c.String(nullable: false, maxLength: 20),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        LastName = c.String(nullable: false, maxLength: 50),
                        Position = c.String(nullable: false, maxLength: 50),
                        Email = c.String(maxLength: 100),
                        Phone = c.String(maxLength: 20),
                        Address = c.String(maxLength: 255),
                        HireDate = c.DateTime(nullable: false),
                        Salary = c.Decimal(precision: 18, scale: 2),
                        Qualifications = c.String(maxLength: 500),
                        LicenseNumber = c.String(maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StaffID)
                .ForeignKey("dbo.Departments", t => t.DepartmentID)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID, unique: true)
                .Index(t => t.DepartmentID)
                .Index(t => t.EmployeeCode, unique: true)
                .Index(t => t.Email, unique: true);
            
            CreateTable(
                "dbo.Departments",
                c => new
                    {
                        DepartmentID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Description = c.String(maxLength: 500),
                        Location = c.String(maxLength: 100),
                        Phone = c.String(maxLength: 20),
                        HeadOfDept = c.Int(),
                        Budget = c.Decimal(precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.DepartmentID);
            
            CreateTable(
                "dbo.Rooms",
                c => new
                    {
                        RoomID = c.Int(nullable: false, identity: true),
                        DepartmentID = c.Int(),
                        RoomNumber = c.String(nullable: false, maxLength: 10),
                        RoomType = c.String(nullable: false, maxLength: 50),
                        Capacity = c.Int(nullable: false),
                        Equipment = c.String(maxLength: 500),
                        Status = c.String(maxLength: 20),
                        Floor = c.Int(),
                        Description = c.String(maxLength: 255),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.RoomID)
                .ForeignKey("dbo.Departments", t => t.DepartmentID)
                .Index(t => t.DepartmentID)
                .Index(t => t.RoomNumber, unique: true);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserID = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        Role = c.String(nullable: false, maxLength: 20),
                        CreatedDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        LastLogin = c.DateTime(),
                        Salt = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.UserID)
                .Index(t => t.Username, unique: true);
            
            CreateTable(
                "dbo.Inventories",
                c => new
                    {
                        ItemID = c.Int(nullable: false, identity: true),
                        SupplierID = c.Int(),
                        ItemCode = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        Category = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 500),
                        CurrentStock = c.Int(nullable: false),
                        MinimumLevel = c.Int(nullable: false),
                        MaximumLevel = c.Int(nullable: false),
                        UnitPrice = c.Decimal(precision: 18, scale: 2),
                        ExpiryDate = c.DateTime(),
                        Location = c.String(maxLength: 100),
                        Status = c.String(maxLength: 20),
                        CreatedDate = c.DateTime(nullable: false),
                        UpdatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ItemID)
                .ForeignKey("dbo.Suppliers", t => t.SupplierID)
                .Index(t => t.SupplierID)
                .Index(t => t.ItemCode, unique: true);
            
            CreateTable(
                "dbo.Suppliers",
                c => new
                    {
                        SupplierID = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false, maxLength: 100),
                        ContactPerson = c.String(maxLength: 100),
                        Phone = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        Address = c.String(maxLength: 255),
                        City = c.String(maxLength: 50),
                        Country = c.String(maxLength: 50),
                        PaymentTerms = c.String(maxLength: 100),
                        Rating = c.Decimal(precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        Notes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.SupplierID);
            
            CreateTable(
                "dbo.PurchaseOrders",
                c => new
                    {
                        OrderID = c.Int(nullable: false, identity: true),
                        SupplierID = c.Int(nullable: false),
                        StaffID = c.Int(),
                        OrderDate = c.DateTime(nullable: false),
                        DeliveryDate = c.DateTime(),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Status = c.String(nullable: false, maxLength: 20),
                        Notes = c.String(maxLength: 1000),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.OrderID)
                .ForeignKey("dbo.Staffs", t => t.StaffID)
                .ForeignKey("dbo.Suppliers", t => t.SupplierID, cascadeDelete: true)
                .Index(t => t.SupplierID)
                .Index(t => t.StaffID);
            
            CreateTable(
                "dbo.OrderDetails",
                c => new
                    {
                        DetailID = c.Int(nullable: false, identity: true),
                        OrderID = c.Int(nullable: false),
                        ItemID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SubTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.DetailID)
                .ForeignKey("dbo.Inventories", t => t.ItemID, cascadeDelete: true)
                .ForeignKey("dbo.PurchaseOrders", t => t.OrderID, cascadeDelete: true)
                .Index(t => t.OrderID)
                .Index(t => t.ItemID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PurchaseOrders", "SupplierID", "dbo.Suppliers");
            DropForeignKey("dbo.PurchaseOrders", "StaffID", "dbo.Staffs");
            DropForeignKey("dbo.OrderDetails", "OrderID", "dbo.PurchaseOrders");
            DropForeignKey("dbo.OrderDetails", "ItemID", "dbo.Inventories");
            DropForeignKey("dbo.Inventories", "SupplierID", "dbo.Suppliers");
            DropForeignKey("dbo.Appointments", "StaffID", "dbo.Staffs");
            DropForeignKey("dbo.Appointments", "PatientID", "dbo.Patients");
            DropForeignKey("dbo.Staffs", "UserID", "dbo.Users");
            DropForeignKey("dbo.MedicalRecords", "StaffID", "dbo.Staffs");
            DropForeignKey("dbo.Staffs", "DepartmentID", "dbo.Departments");
            DropForeignKey("dbo.Rooms", "DepartmentID", "dbo.Departments");
            DropForeignKey("dbo.Appointments", "RoomID", "dbo.Rooms");
            DropForeignKey("dbo.MedicalRecords", "PatientID", "dbo.Patients");
            DropIndex("dbo.OrderDetails", new[] { "ItemID" });
            DropIndex("dbo.OrderDetails", new[] { "OrderID" });
            DropIndex("dbo.PurchaseOrders", new[] { "StaffID" });
            DropIndex("dbo.PurchaseOrders", new[] { "SupplierID" });
            DropIndex("dbo.Inventories", new[] { "ItemCode" });
            DropIndex("dbo.Inventories", new[] { "SupplierID" });
            DropIndex("dbo.Users", new[] { "Username" });
            DropIndex("dbo.Rooms", new[] { "RoomNumber" });
            DropIndex("dbo.Rooms", new[] { "DepartmentID" });
            DropIndex("dbo.Staffs", new[] { "Email" });
            DropIndex("dbo.Staffs", new[] { "EmployeeCode" });
            DropIndex("dbo.Staffs", new[] { "DepartmentID" });
            DropIndex("dbo.Staffs", new[] { "UserID" });
            DropIndex("dbo.MedicalRecords", new[] { "StaffID" });
            DropIndex("dbo.MedicalRecords", new[] { "PatientID" });
            DropIndex("dbo.Patients", new[] { "PatientCode" });
            DropIndex("dbo.Appointments", new[] { "RoomID" });
            DropIndex("dbo.Appointments", new[] { "StaffID" });
            DropIndex("dbo.Appointments", new[] { "PatientID" });
            DropTable("dbo.OrderDetails");
            DropTable("dbo.PurchaseOrders");
            DropTable("dbo.Suppliers");
            DropTable("dbo.Inventories");
            DropTable("dbo.Users");
            DropTable("dbo.Rooms");
            DropTable("dbo.Departments");
            DropTable("dbo.Staffs");
            DropTable("dbo.MedicalRecords");
            DropTable("dbo.Patients");
            DropTable("dbo.Appointments");
        }
    }
}
