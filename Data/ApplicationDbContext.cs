using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Data
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Nouveau modèle
        public DbSet<Alerte> Alerte => Set<Alerte>();
        public DbSet<AlertType> AlertType => Set<AlertType>();
        public DbSet<ExpedType> ExpedType => Set<ExpedType>();
        public DbSet<Etat> Etat => Set<Etat>();
        public DbSet<Statut> Statut => Set<Statut>();
        public DbSet<Destinataire> Destinataire => Set<Destinataire>();
        public DbSet<RappelSuivant> RappelSuivant => Set<RappelSuivant>();
        public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
        public DbSet<ApiClient> ApiClients => Set<ApiClient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WebPushSubscription>(b =>
            {
                b.ToTable("WebPushSubscriptions");
                b.HasKey(x => x.WebPushSubscriptionId);
                b.HasIndex(x => new { x.UserId, x.Endpoint }).IsUnique();
                b.Property(x => x.Endpoint).IsRequired();
                b.Property(x => x.P256dh).IsRequired();
                b.Property(x => x.Auth).IsRequired();
            });

            // Mapping des nouvelles tables
            modelBuilder.Entity<Alerte>(b =>
            {
                b.ToTable("Alerte");
                b.HasKey(x => x.AlerteId);
                b.Property(x => x.TitreAlerte).IsRequired();
                b.HasOne(x => x.AlertType).WithMany().HasForeignKey(x => x.AlertTypeId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.ExpedType).WithMany().HasForeignKey(x => x.ExpedTypeId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.Statut).WithMany().HasForeignKey(x => x.StatutId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.Etat).WithMany().HasForeignKey(x => x.EtatAlerteId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AlertType>(b =>
            {
                b.ToTable("AlertType");
                b.HasKey(x => x.AlertTypeId);
                b.Property(x => x.AlertTypeName)
                    .HasColumnName("AlertType")
                    .IsRequired();
            });

            modelBuilder.Entity<ExpedType>(b =>
            {
                b.ToTable("ExpedType");
                b.HasKey(x => x.ExpedTypeId);
                b.Property(x => x.ExpedTypeName).HasColumnName("ExpedType");
            });

            modelBuilder.Entity<Statut>(b =>
            {
                b.ToTable("Statut");
                b.HasKey(x => x.StatutId);
                b.Property(x => x.StatutName).HasColumnName("Statut");
            });

            modelBuilder.Entity<Etat>(b =>
            {
                b.ToTable("Etat");
                b.HasKey(x => x.EtatAlerteId);
                b.Property(x => x.EtatAlerteName).HasColumnName("EtatAlerte");
            });

            modelBuilder.Entity<Destinataire>(b =>
            {
                b.ToTable("Destinataire");
                b.HasKey(x => x.DestinataireId);
                b.HasOne(x => x.Alerte).WithMany(a => a.Destinataires).HasForeignKey(x => x.AlerteId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RappelSuivant>(b =>
            {
                b.ToTable("RappelSuivant");
                b.HasKey(x => x.RappelId);
                b.HasOne(x => x.Alerte).WithMany(a => a.Rappels).HasForeignKey(x => x.AlerteId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

