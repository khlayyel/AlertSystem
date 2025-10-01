using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Data
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<AlertRecipient> AlertRecipients => Set<AlertRecipient>();
        public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.HasKey(x => x.UserId);
                b.HasIndex(x => x.Email).IsUnique();
                b.Property(x => x.Username).IsRequired();
                b.Property(x => x.Role).IsRequired();
                b.HasOne(x => x.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(x => x.DepartmentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Department>(b =>
            {
                b.ToTable("Departments");
                b.HasKey(x => x.DepartmentId);
                b.Property(x => x.Name).IsRequired();
                b.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<Alert>(b =>
            {
                b.ToTable("Alerts");
                b.HasKey(x => x.AlertId);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.Message).IsRequired();
                b.Property(x => x.AlertType).IsRequired();
                b.HasOne(x => x.Department)
                    .WithMany(d => d.Alerts)
                    .HasForeignKey(x => x.DepartmentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AlertRecipient>(b =>
            {
                b.ToTable("AlertRecipients");
                b.HasKey(x => x.AlertRecipientId);
                b.HasIndex(x => new { x.UserId, x.IsRead, x.IsConfirmed });
                b.HasOne(x => x.Alert)
                    .WithMany(a => a.Recipients)
                    .HasForeignKey(x => x.AlertId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.User)
                    .WithMany(u => u.AlertRecipients)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WebPushSubscription>(b =>
            {
                b.ToTable("WebPushSubscriptions");
                b.HasKey(x => x.WebPushSubscriptionId);
                b.HasIndex(x => new { x.UserId, x.Endpoint }).IsUnique();
                b.Property(x => x.Endpoint).IsRequired();
                b.Property(x => x.P256dh).IsRequired();
                b.Property(x => x.Auth).IsRequired();
            });
        }
    }
}

