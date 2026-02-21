using Microsoft.EntityFrameworkCore;
using Pm.Enums;
using Pm.Models;
using Pm.Models.NEC;
using Pm.Models.SWR;
using Pm.Models.InternalLink;

namespace Pm.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<CallRecord> CallRecords { get; set; } = null!;
        public DbSet<CallSummary> CallSummaries { get; set; } = null!;
        public DbSet<FleetStatistic> FleetStatistics { get; set; } = null!;
        public DbSet<FileImportHistory> FileImportHistories { get; set; } = null!;
        public DbSet<InspeksiTemuanKpc> InspeksiTemuanKpcs { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

        public DbSet<Tower> Towers { get; set; } = null!;
        public DbSet<NecLink> NecLinks { get; set; } = null!;
        public DbSet<NecRslHistory> NecRslHistories { get; set; } = null!;

        public DbSet<SwrSite> SwrSites { get; set; } = null!;
        public DbSet<SwrChannel> SwrChannels { get; set; } = null!;
        public DbSet<SwrHistory> SwrHistories { get; set; } = null!;

        public DbSet<DocumentType> DocumentTypes { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<LetterNumber> LetterNumbers { get; set; } = null!;

        // Radio Management
        public DbSet<RadioTrunking> RadioTrunkings { get; set; } = null!;
        public DbSet<RadioTrunkingHistory> RadioTrunkingHistories { get; set; } = null!;
        public DbSet<RadioConventional> RadioConventionals { get; set; } = null!;
        public DbSet<RadioConventionalHistory> RadioConventionalHistories { get; set; } = null!;
        public DbSet<RadioGrafir> RadioGrafirs { get; set; } = null!;
        public DbSet<RadioScrap> RadioScraps { get; set; } = null!;

        // Gatepass & Quotation
        public DbSet<Gatepass> Gatepasses { get; set; } = null!;
        public DbSet<GatepassItem> GatepassItems { get; set; } = null!;
        public DbSet<Quotation> Quotations { get; set; } = null!;

        // Division Master Data
        public DbSet<Division> Divisions { get; set; } = null!;

        // Internal Link
        public DbSet<InternalLink> InternalLinks { get; set; } = null!;
        public DbSet<InternalLinkHistory> InternalLinkHistories { get; set; } = null!;

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

                entity.Property(e => e.IsActive);

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



            // ===========================================
            // TABEL NEC SIGNAL CONFIGURATION
            // ===========================================

            modelBuilder.Entity<Tower>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("NecTowers");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Location).HasMaxLength(200);
            });

            modelBuilder.Entity<NecLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("NecLinks");
                entity.Property(e => e.LinkName).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.LinkName);

                entity.HasOne(e => e.NearEndTower)
                    .WithMany(t => t.NearEndLinks)
                    .HasForeignKey(e => e.NearEndTowerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FarEndTower)
                    .WithMany(t => t.FarEndLinks)
                    .HasForeignKey(e => e.FarEndTowerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NecRslHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("NecRslHistories");

                // ✅ Status conversion
                entity.Property(h => h.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                // ✅ Notes as nullable TEXT
                entity.Property(e => e.Notes)
                    .IsRequired(false)
                    .HasColumnType("text");

                // ✅ FIX: RslNearEnd menjadi nullable
                entity.Property(e => e.RslNearEnd)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired(false); // ← INI YANG PERLU DIPERBAIKI

                // ✅ RslFarEnd tetap nullable
                entity.Property(e => e.RslFarEnd)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired(false);

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                // ✅ Relations
                entity.HasOne(e => e.NecLink)
                    .WithMany(l => l.Histories)
                    .HasForeignKey(e => e.NecLinkId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ Indexes
                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_NecRslHistory_Date");

                entity.HasIndex(e => new { e.NecLinkId, e.Date })
                    .IsUnique()
                    .HasDatabaseName("IX_NecRslHistory_LinkDate");
            });

            // SwrSite
            modelBuilder.Entity<SwrSite>(entity =>
            {
                entity.ToTable("SwrSites");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Location)
                    .HasMaxLength(255);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasConversion<string>();

                entity.HasIndex(e => e.Name).IsUnique();

                entity.HasMany(e => e.Channels)
                    .WithOne(c => c.SwrSite)
                    .HasForeignKey(c => c.SwrSiteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SwrChannel
            modelBuilder.Entity<SwrChannel>(entity =>
            {
                entity.ToTable("SwrChannels");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ChannelName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ExpectedSwrMax)
                    .HasColumnType("decimal(4,2)")
                    .HasDefaultValue(1.5m);

                entity.Property(e => e.ExpectedPwrMax)
                    .HasColumnType("decimal(6,2)")
                    .IsRequired(false); // nullable

                entity.HasIndex(e => new { e.SwrSiteId, e.ChannelName }).IsUnique();

                entity.HasMany(e => e.Histories)
                    .WithOne(h => h.SwrChannel)
                    .HasForeignKey(h => h.SwrChannelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SwrHistory
            modelBuilder.Entity<SwrHistory>(entity =>
            {
                entity.ToTable("SwrHistories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(e => e.Fpwr)
                    .HasColumnType("decimal(6,2)");

                entity.Property(e => e.Vswr)
                    .IsRequired()
                    .HasColumnType("decimal(4,2)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => new { e.SwrChannelId, e.Date }).IsUnique();
                entity.HasIndex(e => e.Date);
            });

            // ===========================================
            // LETTER NUMBERING SYSTEM CONFIGURATION
            // ===========================================

            // DocumentType
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("DocumentTypes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Company
            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Companies");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Address)
                    .HasMaxLength(500);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // LetterNumber
            modelBuilder.Entity<LetterNumber>(entity =>
            {
                entity.ToTable("LetterNumbers");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FormattedNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.SequenceNumber)
                    .IsRequired();

                entity.Property(e => e.Year)
                    .IsRequired();

                entity.Property(e => e.Month)
                    .IsRequired();

                entity.Property(e => e.LetterDate)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Recipient)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.AttachmentUrl)
                    .HasMaxLength(1000);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                // Unique constraint: One sequence number per (Company, DocumentType, Year)
                entity.HasIndex(e => new { e.CompanyId, e.DocumentTypeId, e.Year, e.SequenceNumber })
                    .IsUnique()
                    .HasDatabaseName("IX_LetterNumber_UniqueSequence");

                // Additional indexes for queries
                entity.HasIndex(e => new { e.Year, e.Month })
                    .HasDatabaseName("IX_LetterNumber_YearMonth");

                entity.HasIndex(e => e.LetterDate)
                    .HasDatabaseName("IX_LetterNumber_LetterDate");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_LetterNumber_Status");

                entity.HasIndex(e => e.FormattedNumber)
                    .HasDatabaseName("IX_LetterNumber_FormattedNumber");

                // Foreign keys
                entity.HasOne(e => e.DocumentType)
                    .WithMany(d => d.LetterNumbers)
                    .HasForeignKey(e => e.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.LetterNumbers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===========================================
            // RADIO MANAGEMENT CONFIGURATION
            // ===========================================

            // RadioGrafir
            modelBuilder.Entity<RadioGrafir>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioGrafirs");

                entity.Property(e => e.NoAsset).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TypeRadio).HasMaxLength(100);
                entity.Property(e => e.Div).HasMaxLength(50);
                entity.Property(e => e.Dept).HasMaxLength(100);
                entity.Property(e => e.FleetId).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");

                entity.HasIndex(e => e.NoAsset).IsUnique().HasDatabaseName("IX_RadioGrafir_NoAsset");
                entity.HasIndex(e => e.SerialNumber).IsUnique().HasDatabaseName("IX_RadioGrafir_SerialNumber");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            // ===========================================
            // GATEPASS & QUOTATION CONFIGURATION
            // ===========================================

            // Gatepass
            modelBuilder.Entity<Gatepass>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Gatepasses");

                entity.Property(e => e.FormattedNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Destination)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PicName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PicContact)
                    .HasMaxLength(50);

                entity.Property(e => e.GatepassDate)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(e => e.SignatureQRCode)
                    .HasMaxLength(200);

                entity.Property(e => e.Notes)
                    .HasColumnType("text");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                // Unique constraint
                entity.HasIndex(e => new { e.Year, e.SequenceNumber })
                    .IsUnique()
                    .HasDatabaseName("IX_Gatepass_UniqueSequence");

                entity.HasIndex(e => e.FormattedNumber)
                    .HasDatabaseName("IX_Gatepass_FormattedNumber");

                entity.HasIndex(e => new { e.Year, e.Month })
                    .HasDatabaseName("IX_Gatepass_YearMonth");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Gatepass_Status");

                // Foreign keys
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SignedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.SignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Child items
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Gatepass)
                    .HasForeignKey(i => i.GatepassId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // GatepassItem
            modelBuilder.Entity<GatepassItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("GatepassItems");

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Quantity)
                    .HasDefaultValue(1);

                entity.Property(e => e.Unit)
                    .HasMaxLength(50)
                    .HasDefaultValue("unit");

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.SerialNumber)
                    .HasMaxLength(100);

                entity.HasIndex(e => e.GatepassId)
                    .HasDatabaseName("IX_GatepassItem_GatepassId");
            });

            // Quotation
            modelBuilder.Entity<Quotation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Quotations");

                entity.Property(e => e.FormattedNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.QuotationDate)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(e => e.Notes)
                    .HasColumnType("text");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                // Unique constraint
                entity.HasIndex(e => new { e.Year, e.SequenceNumber })
                    .IsUnique()
                    .HasDatabaseName("IX_Quotation_UniqueSequence");

                entity.HasIndex(e => e.FormattedNumber)
                    .HasDatabaseName("IX_Quotation_FormattedNumber");

                entity.HasIndex(e => new { e.Year, e.Month })
                    .HasDatabaseName("IX_Quotation_YearMonth");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Quotation_Status");

                entity.HasIndex(e => e.CustomerId)
                    .HasDatabaseName("IX_Quotation_CustomerId");

                // Foreign keys
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RadioTrunking
            modelBuilder.Entity<RadioTrunking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioTrunkings");

                entity.Property(e => e.UnitNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RadioId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.Dept).HasMaxLength(100);
                entity.Property(e => e.Fleet).HasMaxLength(50);
                entity.Property(e => e.RadioType).HasMaxLength(100);
                entity.Property(e => e.JobNumber).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
                entity.Property(e => e.Initiator).HasMaxLength(100);
                entity.Property(e => e.Firmware).HasMaxLength(100);
                entity.Property(e => e.ChannelApply).HasMaxLength(500);

                entity.HasIndex(e => e.RadioId).IsUnique().HasDatabaseName("IX_RadioTrunking_RadioId");
                entity.HasIndex(e => e.UnitNumber).HasDatabaseName("IX_RadioTrunking_UnitNumber");
                entity.HasIndex(e => e.SerialNumber).HasDatabaseName("IX_RadioTrunking_SerialNumber");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasOne(e => e.Grafir)
                    .WithMany(g => g.TrunkingRadios)
                    .HasForeignKey(e => e.GrafirId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Histories)
                    .WithOne(h => h.RadioTrunking)
                    .HasForeignKey(h => h.RadioTrunkingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RadioTrunkingHistory
            modelBuilder.Entity<RadioTrunkingHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioTrunkingHistories");

                entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.RadioTrunkingId).HasDatabaseName("IX_RadioTrunkingHistory_RadioId");
                entity.HasIndex(e => e.ChangedAt).HasDatabaseName("IX_RadioTrunkingHistory_ChangedAt");
            });

            // RadioConventional
            modelBuilder.Entity<RadioConventional>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioConventionals");

                entity.Property(e => e.UnitNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RadioId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.Dept).HasMaxLength(100);
                entity.Property(e => e.Fleet).HasMaxLength(50);
                entity.Property(e => e.RadioType).HasMaxLength(100);
                entity.Property(e => e.Frequency).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");

                entity.HasIndex(e => e.RadioId).IsUnique().HasDatabaseName("IX_RadioConventional_RadioId");
                entity.HasIndex(e => e.UnitNumber).HasDatabaseName("IX_RadioConventional_UnitNumber");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasOne(e => e.Grafir)
                    .WithMany(g => g.ConventionalRadios)
                    .HasForeignKey(e => e.GrafirId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Histories)
                    .WithOne(h => h.RadioConventional)
                    .HasForeignKey(h => h.RadioConventionalId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RadioConventionalHistory
            modelBuilder.Entity<RadioConventionalHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioConventionalHistories");

                entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.RadioConventionalId).HasDatabaseName("IX_RadioConventionalHistory_RadioId");
                entity.HasIndex(e => e.ChangedAt).HasDatabaseName("IX_RadioConventionalHistory_ChangedAt");
            });

            // RadioScrap
            modelBuilder.Entity<RadioScrap>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("RadioScraps");

                entity.Property(e => e.ScrapCategory).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TypeRadio).HasMaxLength(100);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.JobNumber).HasMaxLength(50);
                entity.Property(e => e.DateScrap).IsRequired();

                entity.HasIndex(e => e.ScrapCategory).HasDatabaseName("IX_RadioScrap_Category");
                entity.HasIndex(e => e.DateScrap).HasDatabaseName("IX_RadioScrap_DateScrap");
                entity.HasIndex(e => new { e.ScrapCategory, e.DateScrap }).HasDatabaseName("IX_RadioScrap_Category_Date");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasOne(e => e.SourceTrunking)
                    .WithMany()
                    .HasForeignKey(e => e.SourceTrunkingId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.SourceConventional)
                    .WithMany()
                    .HasForeignKey(e => e.SourceConventionalId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.SourceGrafir)
                    .WithMany()
                    .HasForeignKey(e => e.SourceGrafirId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Division
            modelBuilder.Entity<Division>(entity =>
            {
                entity.ToTable("Divisions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.Code).IsUnique();

                // Foreign keys
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===========================================
            // INTERNAL LINK CONFIGURATION
            // ===========================================

            modelBuilder.Entity<InternalLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("InternalLinks");

                entity.Property(e => e.LinkName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(100);

                entity.Property(e => e.Device)
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .HasMaxLength(100);

                entity.Property(e => e.UsedFrequency)
                    .HasMaxLength(100);

                entity.Property(e => e.RslNearEnd)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired(false);

                entity.Property(e => e.ServiceType)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.LinkName)
                    .HasDatabaseName("IX_InternalLink_LinkName");

                entity.HasMany(e => e.Histories)
                    .WithOne(h => h.InternalLink)
                    .HasForeignKey(h => h.InternalLinkId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InternalLinkHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("InternalLinkHistories");

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.Property(e => e.RslNearEnd)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired(false);

                entity.Property(e => e.Uptime)
                    .IsRequired(false);

                entity.Property(e => e.Notes)
                    .IsRequired(false)
                    .HasColumnType("text");

                entity.Property(e => e.ScreenshotBase64)
                    .IsRequired(false)
                    .HasColumnType("longtext");

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("UTC_TIMESTAMP()");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_InternalLinkHistory_Date");

                entity.HasIndex(e => new { e.InternalLinkId, e.Date })
                    .HasDatabaseName("IX_InternalLinkHistory_LinkDate");
            });
        }
    }
}