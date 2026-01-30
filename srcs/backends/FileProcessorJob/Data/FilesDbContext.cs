using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileProcessorJob.Data;

public class FilesDbContext : DbContext
{
    public FilesDbContext(DbContextOptions<FilesDbContext> options) : base(options)
    {
    }

    public DbSet<FileMetadata> Files { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BlobName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Seed data (same users as TasksApi for consistency)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "demo",
                Email = "demo@example.com",
                PasswordHash = "$2a$11$vZLp.X5K3B3qN8I3KJ9YpO8K9xBxQh8gLI5Y7zR8pF9h1L8N2P3Q4",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = "$2a$11$vZLp.X5K3B3qN8I3KJ9YpO8K9xBxQh8gLI5Y7zR8pF9h1L8N2P3Q5",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 3,
                Username = "user",
                Email = "user@example.com",
                PasswordHash = "$2a$11$vZLp.X5K3B3qN8I3KJ9YpO8K9xBxQh8gLI5Y7zR8pF9h1L8N2P3Q6",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
