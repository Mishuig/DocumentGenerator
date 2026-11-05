using DocumentGenerator.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerator.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public DbSet<TemplateRecord> Templates => Set<TemplateRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={AppPaths.Database}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemplateRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.RelativePath).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64);
            entity.HasIndex(x => x.Name);
        });
    }
}
