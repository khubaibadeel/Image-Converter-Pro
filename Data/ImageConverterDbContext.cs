using ImageConverterPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImageConverterPro.Data
{
    public sealed class ImageConverterDbContext : DbContext
    {
        public DbSet<ConversionHistoryEntity> ConversionHistory => Set<ConversionHistoryEntity>();
        public DbSet<EditingHistoryEntity> EditingHistory => Set<EditingHistoryEntity>();

        public ImageConverterDbContext(DbContextOptions<ImageConverterDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversionHistoryEntity>(entity =>
            {
                entity.ToTable("ConversionHistory");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SourceFile).IsRequired();
                entity.Property(x => x.SourceFormat).IsRequired();
                entity.Property(x => x.OutputFormat).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.HasIndex(x => x.CreatedDate);
                entity.HasIndex(x => x.OutputFormat);
            });

            modelBuilder.Entity<EditingHistoryEntity>(entity =>
            {
                entity.ToTable("EditingHistory");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SourceFile).IsRequired();
                entity.Property(x => x.OutputFile).IsRequired();
                entity.Property(x => x.Operations).IsRequired();
                entity.HasIndex(x => x.CreatedDate);
            });
        }
    }
}
