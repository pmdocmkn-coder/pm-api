using Microsoft.EntityFrameworkCore;
using Pm.Models;

namespace Pm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<CallRecord> CallRecords { get; set; }
        public DbSet<CallSummary> CallSummaries { get; set; }
        public DbSet<FleetStatistic> FleetStatistics { get; set; }
        public DbSet<FileImportHistory> FileImportHistories { get; set; }
        public DbSet<InspeksiTemuanKpc> InspeksiTemuanKpcs { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Optimizations untuk bulk operations
            optionsBuilder
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // Default no tracking
                .EnableSensitiveDataLogging(false) // Disable untuk performa
                .EnableDetailedErrors(false); // Disable untuk performa
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===========================================
            // ✅ USER CONFIGURATION - PERBAIKI!
            // ===========================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.ToTable("Users");
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(e => e.Email)
                    .HasMaxLength(200);
                
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);
                
                entity.Property(e => e.PhotoUrl)
                    .HasMaxLength(500);
                
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(false);
                
                entity.Property(e => e.LastLogin)
                    .IsRequired(false);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
                
                entity.Property(e => e.UpdatedAt)
                    .IsRequired(false);

                // ✅ TAMBAHKAN RELASI KE ActivityLogs
                entity.HasMany(u => u.ActivityLogs)
                    .WithOne(al => al.User)
                    .HasForeignKey(al => al.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===========================================
            // ✅ ACTIVITY LOG CONFIGURATION - TAMBAHKAN!
            // ===========================================
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ActivityLogs");
                
                entity.Property(e => e.Module)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                entity.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
                
                entity.Property(e => e.EntityId)
                    .IsRequired(false);
                
                entity.Property(e => e.UserId)
                    .IsRequired();

                // ✅ FOREIGN KEY KE USER - PASTIKAN INI ADA!
                entity.HasOne(al => al.User)
                    .WithMany(u => u.ActivityLogs)
                    .HasForeignKey(al => al.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index untuk query performance
                entity.HasIndex(e => new { e.Module, e.Timestamp })
                    .HasDatabaseName("IX_ActivityLog_Module_Time");
                
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_ActivityLog_UserId");
            });

            // ===========================================
            // ROLE CONFIGURATION
            // ===========================================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.ToTable("Roles");
                
                entity.HasIndex(e => e.RoleName)
                    .IsUnique()
                    .HasDatabaseName("IX_Role_RoleName");
                
                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
                
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            // ===========================================
            // PERMISSION CONFIGURATION
            // ===========================================
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.PermissionId);
                entity.ToTable("Permissions");
                
                entity.HasIndex(e => e.PermissionName)
                    .IsUnique()
                    .HasDatabaseName("IX_Permission_PermissionName");
                
                entity.Property(e => e.PermissionName)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
                
                entity.Property(e => e.Group)
                    .HasMaxLength(50);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            // ===========================================
            // ROLE PERMISSION CONFIGURATION
            // ===========================================
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.RolePermissionId);
                entity.ToTable("RolePermissions");
                
                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Unique constraint untuk kombinasi RoleId dan PermissionId
                entity.HasIndex(e => new { e.RoleId, e.PermissionId })
                    .IsUnique()
                    .HasDatabaseName("IX_RolePermission_RoleId_PermissionId");
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            // ===========================================
            // INSPEKSI TEMUAN KPC CONFIGURATION
            // ===========================================
            modelBuilder.Entity<InspeksiTemuanKpc>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("InspeksiTemuanKpcs");
                
                entity.Property(e => e.Ruang)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(e => e.Temuan)
                    .IsRequired();
                
                entity.Property(e => e.KategoriTemuan)
                    .HasMaxLength(200);
                
                entity.Property(e => e.Inspector)
                    .HasMaxLength(200);
                
                entity.Property(e => e.Severity)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Medium");
                
                entity.Property(e => e.NoFollowUp)
                    .HasMaxLength(100);
                
                entity.Property(e => e.PerbaikanDilakukan);
                
                entity.Property(e => e.PicPelaksana)
                    .HasMaxLength(200);
                
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Open");
                
                entity.Property(e => e.Keterangan);
                
                entity.Property(e => e.FotoTemuanUrls);
                entity.Property(e => e.FotoHasilUrls);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");
                
                entity.Property(e => e.UpdatedAt)
                    .IsRequired(false);
                
                entity.Property(e => e.DeletedAt)
                    .IsRequired(false);
                
                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);

                // Foreign keys
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.DeletedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.DeletedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(e => new { e.IsDeleted, e.Status })
                    .HasDatabaseName("IX_InspeksiTemuanKpc_Deleted_Status");
                
                entity.HasIndex(e => e.Ruang)
                    .HasDatabaseName("IX_InspeksiTemuanKpc_Ruang");
                
                entity.HasIndex(e => e.TanggalTemuan)
                    .HasDatabaseName("IX_InspeksiTemuanKpc_Tanggal");
            });

            // ===========================================
            // TABEL LAINNYA (CallRecord, CallSummary, dll)
            // ===========================================
            modelBuilder.Entity<CallRecord>(entity =>
            {
                entity.HasKey(e => e.CallRecordId);
                entity.ToTable("CallRecords");
                
                entity.HasIndex(e => new { e.CallDate, e.CallTime })
                    .HasDatabaseName("IX_CallRecord_DateTime");
                
                entity.HasIndex(e => e.CallCloseReason)
                    .HasDatabaseName("IX_CallRecord_CloseReason");
                
                entity.HasIndex(e => e.CallDate)
                    .HasDatabaseName("IX_CallRecord_Date");
            });

            modelBuilder.Entity<CallSummary>(entity =>
            {
                entity.HasKey(e => e.CallSummaryId);
                entity.ToTable("CallSummaries");
                
                entity.HasIndex(e => new { e.SummaryDate, e.HourGroup })
                    .IsUnique()
                    .HasDatabaseName("IX_CallSummary_DateHour");
                
                entity.Property(e => e.TEBusyPercent)
                    .HasColumnType("decimal(5,2)");
                
                entity.Property(e => e.SysBusyPercent)
                    .HasColumnType("decimal(5,2)");
                
                entity.Property(e => e.OthersPercent)
                    .HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<FleetStatistic>(entity =>
            {
                entity.HasKey(e => e.FleetStatisticId);
                entity.ToTable("FleetStatistics");
            });

            modelBuilder.Entity<FileImportHistory>(entity =>
            {
                entity.HasKey(e => e.ImportHistoryId);
                entity.ToTable("FileImportHistories");
            });
        }
    }
}