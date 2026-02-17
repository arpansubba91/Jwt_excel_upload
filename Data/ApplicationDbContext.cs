using Microsoft.EntityFrameworkCore;
using mySystem.Models;

namespace mySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Country>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Capital)
                    .HasMaxLength(100);

                entity.Property(e => e.Region)
                    .HasMaxLength(100);

                entity.Property(e => e.Population)
                    .HasMaxLength(50);

                entity.Property(e => e.UploadDateTime)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.UploadedByUserId);
                entity.HasIndex(e => e.UploadDateTime);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Username).IsUnique();
            });
        }
    }
}