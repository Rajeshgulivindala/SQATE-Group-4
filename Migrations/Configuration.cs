using System.Data.Entity.Migrations;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<HMSDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(HMSDbContext context)
        {
            // Create default admin user
            context.Users.AddOrUpdate(u => u.Username,
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin",
                    IsActive = true,
                    CreatedDate = System.DateTime.Now
                }
            );

            // Create sample departments
            context.Departments.AddOrUpdate(d => d.Name,
                new Department
                {
                    Name = "Emergency",
                    Description = "Emergency Department",
                    Location = "Ground Floor",
                    Phone = "555-0001",
                    IsActive = true
                },
                new Department
                {
                    Name = "Cardiology",
                    Description = "Heart and Cardiovascular Care",
                    Location = "Second Floor",
                    Phone = "555-0002",
                    IsActive = true
                }
            );

            // Create sample rooms
            context.Rooms.AddOrUpdate(r => r.RoomNumber,
                new Room
                {
                    RoomNumber = "101",
                    RoomType = "General",
                    Capacity = 2,
                    Status = "Available",
                    Floor = 1
                },
                new Room
                {
                    RoomNumber = "201",
                    RoomType = "ICU",
                    Capacity = 1,
                    Status = "Available",
                    Floor = 2
                }
            );

            context.SaveChanges();
        }
    }
}
