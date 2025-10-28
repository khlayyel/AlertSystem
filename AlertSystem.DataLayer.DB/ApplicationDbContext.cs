using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using AlertSystem.DataLayer.Interfaces;

namespace AlertSystem.Data
{
    public sealed class ApplicationDbContext : DbContext, IDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Nouveau modèle refactorisé
        public DbSet<Alerte> Alerte => Set<Alerte>();
        public DbSet<AlertType> AlertType => Set<AlertType>();
        public DbSet<Etat> Etat => Set<Etat>();
        public DbSet<Statut> Statut => Set<Statut>();
        public DbSet<RappelSuivant> RappelSuivant => Set<RappelSuivant>();
        public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
        public DbSet<ApiClient> ApiClients => Set<ApiClient>();
        // Hotel user table (read-only)
        public DbSet<DefUtilisateur> DefUtilisateurs => Set<DefUtilisateur>();
        public DbSet<PlateformeEnvoie> PlateformeEnvoie => Set<PlateformeEnvoie>();
        public DbSet<AlertProcessingQueue> AlertProcessingQueue => Set<AlertProcessingQueue>();

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

            // Configuration de la nouvelle entité Alerte refactorisée
            modelBuilder.Entity<Alerte>(b =>
            {
                b.ToTable("Alerte");
                b.HasKey(x => x.AlertRecordId);
                b.Property(x => x.AlertRecordId).HasColumnName("AlertRecordId").ValueGeneratedOnAdd();
                b.Property(x => x.AlertGroupId).IsRequired();
                b.Property(x => x.TitreAlerte).IsRequired();
                b.Property(x => x.PlateformeEnvoieId).IsRequired();
                b.Property(x => x.ProcessedByWorker).HasDefaultValue(false);
                b.Property(x => x.AttemptCount).HasDefaultValue(0);
                
                // Index pour les performances
                b.HasIndex(x => x.AlertGroupId);
                b.HasIndex(x => new { x.StatutId, x.ProcessedByWorker, x.DateCreationAlerte });
                
                // Relations
                b.HasOne(x => x.AlertType).WithMany().HasForeignKey(x => x.AlertTypeId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.Statut).WithMany().HasForeignKey(x => x.StatutId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.Etat).WithMany().HasForeignKey(x => x.EtatAlerteId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.Expediteur).WithMany().HasForeignKey(x => x.ExpediteurId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.PlateformeEnvoie).WithMany().HasForeignKey(x => x.PlateformeEnvoieId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(x => x.DestinataireUser).WithMany().HasForeignKey(x => x.DestinataireUserId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AlertType>(b =>
            {
                b.ToTable("AlertType");
                b.HasKey(x => x.AlertTypeId);
                b.Property(x => x.AlertTypeName)
                    .HasColumnName("AlertType")
                    .IsRequired();
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


            modelBuilder.Entity<RappelSuivant>(b =>
            {
                b.ToTable("RappelSuivant");
                b.HasKey(x => x.RappelId);
                b.Property(x => x.DateRappel).HasDefaultValueSql("GETUTCDATE()");
                b.Property(x => x.StatutRappel).HasMaxLength(50);
                b.HasOne(x => x.Alerte).WithMany(a => a.Rappels).HasForeignKey(x => x.AlerteId).OnDelete(DeleteBehavior.Cascade);
            });

            // Hotel user table mapping (read-only)
            modelBuilder.Entity<DefUtilisateur>(b =>
            {
                b.ToTable("def_utilisateur");
                b.HasKey(x => x.util_id);
                b.Property(x => x.util_id).HasColumnType("numeric(4,0)");
                b.Property(x => x.util_nom).HasColumnName("util_nom");
                b.Property(x => x.util_prenom).HasColumnName("util_prenom");
                b.Property(x => x.util_login).HasColumnName("util_login");
                b.Property(x => x.util_password).HasColumnName("util_password");
                b.Property(x => x.util_fonction).HasColumnName("util_fonction");
                b.Property(x => x.util_email).HasColumnName("util_email");
                b.Property(x => x.util_date_expiration_mdp).HasColumnName("util_date_expiration_mdp");
                b.Property(x => x.util_compte_active).HasColumnName("util_compte_active");
                b.Property(x => x.util_get_fond_caisse).HasColumnName("util_get_fond_caisse");
                b.Property(x => x.util_code_operateur).HasColumnName("util_code_operateur");
                b.Property(x => x.util_work_station).HasColumnName("util_work_station");
                b.Property(x => x.util_user_login).HasColumnName("util_user_login");
                b.Property(x => x.langue_id).HasColumnName("langue_id");
                b.Property(x => x.util_signature_name).HasColumnName("util_signature_name");
                b.Property(x => x.util_signature).HasColumnName("util_signature");
            });

            modelBuilder.Entity<PlateformeEnvoie>(b =>
            {
                b.ToTable("PlateformeEnvoie");
                b.HasKey(x => x.PlateformeId);
                b.Property(x => x.Plateforme).IsRequired().HasMaxLength(50);
            });

            // Configuration de la table de queue
            modelBuilder.Entity<AlertProcessingQueue>(b =>
            {
                b.ToTable("AlertProcessingQueue");
                b.HasKey(x => x.QueueId);
                b.Property(x => x.QueueId).ValueGeneratedOnAdd();
                b.Property(x => x.QueuedAt).HasDefaultValueSql("GETUTCDATE()");
                b.Property(x => x.Priority).HasDefaultValue(0);
                b.Property(x => x.RetryCount).HasDefaultValue(0);
                
                // Index pour les performances
                b.HasIndex(x => new { x.Priority, x.QueuedAt });
                b.HasIndex(x => x.AlertRecordId);
            });
        }
    }
}

