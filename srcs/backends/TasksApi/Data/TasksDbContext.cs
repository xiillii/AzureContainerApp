using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace TasksApi.Data;

public class TasksDbContext : DbContext
{
    public TasksDbContext(DbContextOptions<TasksDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Seed data
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                // Password: "admin123" (you should use a proper password hashing in production)
                PasswordHash = "$2a$11$vZLp.X5K3B3qN8I3KJ9YpO8K9xBxQh8gLI5Y7zR8pF9h1L8N2P3Q4",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "user",
                Email = "user@example.com",
                // Password: "user123"
                PasswordHash = "$2a$11$vZLp.X5K3B3qN8I3KJ9YpO8K9xBxQh8gLI5Y7zR8pF9h1L8N2P3Q5",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
