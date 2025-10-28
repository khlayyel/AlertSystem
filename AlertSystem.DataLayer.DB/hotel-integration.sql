IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AlertProcessingQueue] (
    [QueueId] int NOT NULL IDENTITY,
    [AlertRecordId] int NOT NULL,
    [QueuedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [Priority] int NOT NULL DEFAULT 0,
    [RetryCount] int NOT NULL DEFAULT 0,
    [LastAttemptAt] datetime2 NULL,
    [LastError] nvarchar(max) NULL,
    CONSTRAINT [PK_AlertProcessingQueue] PRIMARY KEY ([QueueId])
);

CREATE TABLE [AlertType] (
    [AlertTypeId] int NOT NULL IDENTITY,
    [AlertType] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [RequiresAcknowledgment] bit NOT NULL,
    CONSTRAINT [PK_AlertType] PRIMARY KEY ([AlertTypeId])
);

CREATE TABLE [ApiClients] (
    [ApiClientId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ApiKeyHash] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RateLimitPerMinute] int NULL,
    CONSTRAINT [PK_ApiClients] PRIMARY KEY ([ApiClientId])
);

CREATE TABLE [def_utilisateur] (
    [util_id] int NOT NULL IDENTITY,
    [util_nom] nvarchar(max) NOT NULL,
    [util_email] nvarchar(max) NULL,
    [util_telephone_portable] nvarchar(max) NULL,
    [util_actif] bit NOT NULL,
    CONSTRAINT [PK_def_utilisateur] PRIMARY KEY ([util_id])
);

CREATE TABLE [Etat] (
    [EtatAlerteId] int NOT NULL IDENTITY,
    [EtatAlerte] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Etat] PRIMARY KEY ([EtatAlerteId])
);

CREATE TABLE [PlateformeEnvoie] (
    [PlateformeId] int NOT NULL IDENTITY,
    [Plateforme] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_PlateformeEnvoie] PRIMARY KEY ([PlateformeId])
);

CREATE TABLE [Statut] (
    [StatutId] int NOT NULL IDENTITY,
    [Statut] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Statut] PRIMARY KEY ([StatutId])
);

CREATE TABLE [WebPushSubscriptions] (
    [WebPushSubscriptionId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Endpoint] nvarchar(450) NOT NULL,
    [P256dh] nvarchar(max) NOT NULL,
    [Auth] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WebPushSubscriptions] PRIMARY KEY ([WebPushSubscriptionId])
);

CREATE TABLE [Alerte] (
    [AlertRecordId] int NOT NULL IDENTITY,
    [AlertGroupId] uniqueidentifier NOT NULL,
    [AlertTypeId] int NOT NULL,
    [AppId] int NULL,
    [ExpediteurId] int NULL,
    [TitreAlerte] nvarchar(max) NOT NULL,
    [DescriptionAlerte] nvarchar(max) NULL,
    [DateCreationAlerte] datetime2 NOT NULL,
    [StatutId] int NOT NULL,
    [EtatAlerteId] int NOT NULL,
    [PlateformeEnvoieId] int NOT NULL,
    [DestinataireUserId] int NULL,
    [DestinataireEmail] nvarchar(max) NULL,
    [DestinatairePhoneNumber] nvarchar(max) NULL,
    [DestinataireDesktop] nvarchar(max) NULL,
    [DateLecture] datetime2 NULL,
    [RappelSuivant] datetime2 NULL,
    [ProcessedByWorker] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PlateformeEnvoiePlateformeId] int NULL,
    CONSTRAINT [PK_Alerte] PRIMARY KEY ([AlertRecordId]),
    CONSTRAINT [FK_Alerte_AlertType_AlertTypeId] FOREIGN KEY ([AlertTypeId]) REFERENCES [AlertType] ([AlertTypeId]),
    CONSTRAINT [FK_Alerte_Etat_EtatAlerteId] FOREIGN KEY ([EtatAlerteId]) REFERENCES [Etat] ([EtatAlerteId]),
    CONSTRAINT [FK_Alerte_PlateformeEnvoie_PlateformeEnvoieId] FOREIGN KEY ([PlateformeEnvoieId]) REFERENCES [PlateformeEnvoie] ([PlateformeId]),
    CONSTRAINT [FK_Alerte_PlateformeEnvoie_PlateformeEnvoiePlateformeId] FOREIGN KEY ([PlateformeEnvoiePlateformeId]) REFERENCES [PlateformeEnvoie] ([PlateformeId]),
    CONSTRAINT [FK_Alerte_Statut_StatutId] FOREIGN KEY ([StatutId]) REFERENCES [Statut] ([StatutId]),
    CONSTRAINT [FK_Alerte_def_utilisateur_DestinataireUserId] FOREIGN KEY ([DestinataireUserId]) REFERENCES [def_utilisateur] ([util_id]),
    CONSTRAINT [FK_Alerte_def_utilisateur_ExpediteurId] FOREIGN KEY ([ExpediteurId]) REFERENCES [def_utilisateur] ([util_id])
);

CREATE TABLE [RappelSuivant] (
    [RappelId] int NOT NULL IDENTITY,
    [AlerteId] int NOT NULL,
    [AlertRecordId] int NOT NULL,
    [DateRappel] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [StatutRappel] nvarchar(50) NULL,
    [Tentative] int NOT NULL,
    [DetailsErreur] nvarchar(max) NULL,
    CONSTRAINT [PK_RappelSuivant] PRIMARY KEY ([RappelId]),
    CONSTRAINT [FK_RappelSuivant_Alerte_AlerteId] FOREIGN KEY ([AlerteId]) REFERENCES [Alerte] ([AlertRecordId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Alerte_AlertGroupId] ON [Alerte] ([AlertGroupId]);

CREATE INDEX [IX_Alerte_AlertTypeId] ON [Alerte] ([AlertTypeId]);

CREATE INDEX [IX_Alerte_DestinataireUserId] ON [Alerte] ([DestinataireUserId]);

CREATE INDEX [IX_Alerte_EtatAlerteId] ON [Alerte] ([EtatAlerteId]);

CREATE INDEX [IX_Alerte_ExpediteurId] ON [Alerte] ([ExpediteurId]);

CREATE INDEX [IX_Alerte_PlateformeEnvoieId] ON [Alerte] ([PlateformeEnvoieId]);

CREATE INDEX [IX_Alerte_PlateformeEnvoiePlateformeId] ON [Alerte] ([PlateformeEnvoiePlateformeId]);

CREATE INDEX [IX_Alerte_StatutId_ProcessedByWorker_DateCreationAlerte] ON [Alerte] ([StatutId], [ProcessedByWorker], [DateCreationAlerte]);

CREATE INDEX [IX_AlertProcessingQueue_AlertRecordId] ON [AlertProcessingQueue] ([AlertRecordId]);

CREATE INDEX [IX_AlertProcessingQueue_Priority_QueuedAt] ON [AlertProcessingQueue] ([Priority], [QueuedAt]);

CREATE INDEX [IX_RappelSuivant_AlerteId] ON [RappelSuivant] ([AlerteId]);

CREATE UNIQUE INDEX [IX_WebPushSubscriptions_UserId_Endpoint] ON [WebPushSubscriptions] ([UserId], [Endpoint]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251026192758_AddAlertSystemSchemaToHotelDB', N'9.0.10');

COMMIT;
GO

