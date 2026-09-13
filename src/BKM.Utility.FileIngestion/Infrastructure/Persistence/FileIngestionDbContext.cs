using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.FileIngestion.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the FileIngestion feature.
/// Owns: IngestionBatches, IngestedRecords.
/// Completely independent — no other feature's tables are present.
/// Connection string key: "FileIngestion" (falls back to "DefaultConnection").
/// </summary>
public sealed class FileIngestionDbContext(DbContextOptions<FileIngestionDbContext> options)
    : DbContext(options)
{
    public DbSet<IngestionBatch>  IngestionBatches  => Set<IngestionBatch>();
    public DbSet<IngestedRecord>  IngestedRecords   => Set<IngestedRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IngestionBatch>(e =>
        {
            e.ToTable("IngestionBatches", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RecordType).HasMaxLength(200);
            e.Property(x => x.ParseOptions).HasColumnType("nvarchar(max)");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.StartedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_IngestionBatches_Status");
            e.HasIndex(x => x.StartedAt).HasDatabaseName("IX_IngestionBatches_StartedAt");
        });

        modelBuilder.Entity<IngestedRecord>(e =>
        {
            e.ToTable("IngestedRecords", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.BatchId).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RecordType).HasMaxLength(200);
            e.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.IngestedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.BatchId, x.RowNumber }).HasDatabaseName("IX_IngestedRecords_BatchId_RowNumber");
            e.HasIndex(x => new { x.BatchId, x.PayloadHash }).HasDatabaseName("IX_IngestedRecords_BatchId_PayloadHash");
            e.HasIndex(x => new { x.FileType, x.RecordType }).HasDatabaseName("IX_IngestedRecords_FileType_RecordType");
            e.HasIndex(x => x.IngestedAt).HasDatabaseName("IX_IngestedRecords_IngestedAt");
        });
    }
}
