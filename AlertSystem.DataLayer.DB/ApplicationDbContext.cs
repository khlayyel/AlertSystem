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

        // Main tables
        public DbSet<Alerte> Alerte => Set<Alerte>();
        public DbSet<RappelSuivant> RappelSuivant => Set<RappelSuivant>();
        public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
        public DbSet<ApiClient> ApiClients => Set<ApiClient>();
        public DbSet<AlertProcessingQueue> AlertProcessingQueue => Set<AlertProcessingQueue>();

        // Reference tables
        public DbSet<DefApp> DefApp => Set<DefApp>();
        public DbSet<DefTypeEnvoie> DefTypeEnvoie => Set<DefTypeEnvoie>();
        public DbSet<DefTypeAlerte> DefTypeAlerte => Set<DefTypeAlerte>();
        public DbSet<DefUtilisateur> DefUtilisateur => Set<DefUtilisateur>();
        public DbSet<DefAlerte> DefAlerte => Set<DefAlerte>();
        public DbSet<Statut> Statut => Set<Statut>();
        public DbSet<Etat> Etat => Set<Etat>();
        public DbSet<PlateformeEnvoie> PlateformeEnvoie => Set<PlateformeEnvoie>();

        // Hotel user table (read-only) - renamed to avoid conflict
        public DbSet<HotelDefUtilisateur> HotelDefUtilisateurs => Set<HotelDefUtilisateur>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WebPushSubscription
            modelBuilder.Entity<WebPushSubscription>(b =>
            {
                b.ToTable("WebPushSubscriptions");
                b.HasKey(x => x.WebPushSubscriptionId);
                b.HasIndex(x => new { x.UserId, x.Endpoint }).IsUnique();
                b.Property(x => x.Endpoint).IsRequired();
                b.Property(x => x.P256dh).IsRequired();
                b.Property(x => x.Auth).IsRequired();
            });

            // DefApp
            modelBuilder.Entity<DefApp>(b =>
            {
                b.ToTable("def_App");
                b.HasKey(x => x.AppId);
                b.Property(x => x.AppId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
            });

            // DefTypeEnvoie
            modelBuilder.Entity<DefTypeEnvoie>(b =>
            {
                b.ToTable("def_TypeEnvoie");
                b.HasKey(x => x.TypeEnvoieId);
                b.Property(x => x.TypeEnvoieId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
            });

            // DefTypeAlerte
            modelBuilder.Entity<DefTypeAlerte>(b =>
            {
                b.ToTable("def_TypeAlerte");
                b.HasKey(x => x.TypeAlertId);
                b.Property(x => x.TypeAlertId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
                b.HasOne(x => x.App).WithMany(a => a.TypeAlertes).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Restrict);
            });

            // DefUtilisateur (new table for alert system users)
            modelBuilder.Entity<DefUtilisateur>(b =>
            {
                b.ToTable("def_Utilisateur");
                b.HasKey(x => x.UtilisateurId);
                b.Property(x => x.UtilisateurId).ValueGeneratedOnAdd();
                b.Property(x => x.Username).IsRequired().HasMaxLength(100);
                b.Property(x => x.Password).IsRequired().HasMaxLength(255);
                b.Property(x => x.Email).IsRequired().HasMaxLength(255);
                b.Property(x => x.WhatsAppNumber).HasMaxLength(20);
                b.HasIndex(x => x.Email).IsUnique();
                b.HasIndex(x => x.WhatsAppNumber);
                b.HasOne(x => x.App).WithMany(a => a.Utilisateurs).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Restrict);
            });

            // DefAlerte
            modelBuilder.Entity<DefAlerte>(b =>
            {
                b.ToTable("def_Alerte");
                b.HasKey(x => x.DefAlerteId);
                b.Property(x => x.DefAlerteId).ValueGeneratedOnAdd();
                b.Property(x => x.ListDestinatairesId).IsRequired();
                b.Property(x => x.URL).IsRequired().HasMaxLength(500);
                b.Property(x => x.IsActive).HasDefaultValue(true);
                b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                b.HasIndex(x => x.IsActive);
                b.HasIndex(x => x.DefTypeAlerte);
                b.HasOne(x => x.TypeAlerte).WithMany(t => t.Alertes).HasForeignKey(x => x.DefTypeAlerte).OnDelete(DeleteBehavior.Restrict);
            });

            // Statut
            modelBuilder.Entity<Statut>(b =>
            {
                b.ToTable("def_Statut");
                b.HasKey(x => x.StatutId);
                b.Property(x => x.StatutId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
            });

            // Etat
            modelBuilder.Entity<Etat>(b =>
            {
                b.ToTable("def_Etat");
                b.HasKey(x => x.EtatId);
                b.Property(x => x.EtatId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
            });

            // PlateformeEnvoie
            modelBuilder.Entity<PlateformeEnvoie>(b =>
            {
                b.ToTable("def_PlateformeEnvoi");
                b.HasKey(x => x.PlateformeId);
                b.Property(x => x.PlateformeId).ValueGeneratedNever(); // Manual ID
                b.Property(x => x.Description).IsRequired().HasMaxLength(100);
            });

            // Alerte
            modelBuilder.Entity<Alerte>(b =>
            {
                b.ToTable("Alerte");
                b.HasKey(x => x.AlertRecordId);
                b.Property(x => x.AlertRecordId).HasColumnName("AlertRecordId").ValueGeneratedOnAdd();
                b.Property(x => x.AlertGroupId).IsRequired();
                b.Property(x => x.AppId).IsRequired();
                b.Property(x => x.TypeEnvoieId).IsRequired();
                b.Property(x => x.TitreAlerte).IsRequired().HasMaxLength(255);
                b.Property(x => x.Destinataire).IsRequired().HasMaxLength(255);
                b.Property(x => x.PlateformeEnvoieId).IsRequired();
                b.Property(x => x.ProcessedByWorker).HasDefaultValue(false);
                b.Property(x => x.AttemptCount).HasDefaultValue(0);
                
                // Indexes
                b.HasIndex(x => x.AlertGroupId);
                b.HasIndex(x => new { x.StatutId, x.ProcessedByWorker, x.DateCreationAlerte });
                b.HasIndex(x => new { x.AppId, x.DateCreationAlerte });
                
                // Foreign keys
                b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.TypeEnvoie).WithMany().HasForeignKey(x => x.TypeEnvoieId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Statut).WithMany().HasForeignKey(x => x.StatutId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Etat).WithMany().HasForeignKey(x => x.EtatId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.PlateformeEnvoie).WithMany(p => p.Alertes).HasForeignKey(x => x.PlateformeEnvoieId).OnDelete(DeleteBehavior.Restrict);
            });

            // RappelSuivant
            modelBuilder.Entity<RappelSuivant>(b =>
            {
                b.ToTable("RappelSuivant");
                b.HasKey(x => x.RappelId);
                b.Property(x => x.DateRappel).HasDefaultValueSql("GETUTCDATE()");
                b.Property(x => x.StatutRappel).HasMaxLength(50);
                b.HasOne(x => x.Alerte).WithMany(a => a.Rappels).HasForeignKey(x => x.AlerteId).OnDelete(DeleteBehavior.Cascade);
            });

            // Hotel user table mapping (read-only) - renamed to avoid conflict
            modelBuilder.Entity<HotelDefUtilisateur>(b =>
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

            // AlertProcessingQueue
            modelBuilder.Entity<AlertProcessingQueue>(b =>
            {
                b.ToTable("AlertProcessingQueue");
                b.HasKey(x => x.QueueId);
                b.Property(x => x.QueueId).ValueGeneratedOnAdd();
                b.Property(x => x.QueuedAt).HasDefaultValueSql("GETUTCDATE()");
                b.Property(x => x.Priority).HasDefaultValue(0);
                b.Property(x => x.RetryCount).HasDefaultValue(0);
                
                // Indexes
                b.HasIndex(x => new { x.Priority, x.QueuedAt });
                b.HasIndex(x => x.AlertRecordId);
            });
        }
    }
}
