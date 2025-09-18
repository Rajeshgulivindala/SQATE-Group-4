using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Services.Data
{
    public class HMSDbContext : DbContext
    {
        public HMSDbContext() : base("DefaultConnection")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Simple relationship configurations

            // Patient -> Appointments (1:Many)
            modelBuilder.Entity<Appointment>()
                .HasRequired(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientID);

            // Staff -> Appointments (1:Many)
            modelBuilder.Entity<Appointment>()
                .HasRequired(a => a.Staff)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.StaffID);

            base.OnModelCreating(modelBuilder);
        }
    }
}