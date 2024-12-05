
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace WebProgramlamaProje.Models
{
    public class SalonDbContext : DbContext
    {
        // Veritabanı tablolarını temsil eden DbSet özellikleri
        public SalonDbContext(DbContextOptions<SalonDbContext> options) : base(options)
        {
        }
        public DbSet<Salon> Salons { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Employee> Employees { get; set; }
        
        public DbSet<User> Users { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<WorkingHour> WorkingHours { get; set; } // Çalışma saatleri tablosu

        // PostgreSQL bağlantı ayarları
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=KuaforDB;Username=postgres;Password=gardolap41");
        }

        // Veritabanı yapılandırma işlemleri
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Salon ve Employee ilişkisi (Salon silindiğinde çalışanlar da silinir)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Salon) // Her Employee bir Salon'a ait
                .WithMany(s => s.Employees) // Bir Salon'un birden fazla Employee'si olabilir
                .HasForeignKey(e => e.SalonId) // Employee tablosundaki yabancı anahtar
                .OnDelete(DeleteBehavior.Cascade); // Salon silindiğinde çalışanlar da silinsin

            // Salon ve WorkingHour ilişkisi (Salon silindiğinde çalışma saatleri de silinir)
            modelBuilder.Entity<WorkingHour>()
                .HasOne(wh => wh.Salon) // Her WorkingHour bir Salon'a ait
                .WithMany(s => s.WorkingHours) // Bir Salon'un birden fazla WorkingHour'u olabilir
                .HasForeignKey(wh => wh.SalonId) // WorkingHour tablosundaki yabancı anahtar
                .OnDelete(DeleteBehavior.Cascade); // Salon silindiğinde çalışma saatleri de silinsin

            // Employee ve WorkingHour ilişkisi (Çalışan silindiğinde çalışma saatleri de silinir)
            modelBuilder.Entity<WorkingHour>()
                .HasOne(wh => wh.Employee) // Her WorkingHour bir Employee'ye ait olabilir
                .WithMany(e => e.WorkingHours) // Bir Employee'nin birden fazla WorkingHour'u olabilir
                .HasForeignKey(wh => wh.EmployeeId) // WorkingHour tablosundaki yabancı anahtar
                .OnDelete(DeleteBehavior.Cascade); // Çalışan silindiğinde çalışma saatleri de silinsin

            base.OnModelCreating(modelBuilder);
        }



    }
}