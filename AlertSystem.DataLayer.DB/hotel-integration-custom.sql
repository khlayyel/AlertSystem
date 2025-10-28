-- Script d'intégration AlertSystem dans la base de données hôtel
-- Ce script ajoute les tables AlertSystem sans recréer def_utilisateur

-- Vérifier si les tables n'existent pas déjà
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AlertProcessingQueue' AND xtype='U')
BEGIN
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
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AlertType' AND xtype='U')
BEGIN
    CREATE TABLE [AlertType] (
        [AlertTypeId] int NOT NULL IDENTITY,
        [AlertType] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [RequiresAcknowledgment] bit NOT NULL,
        CONSTRAINT [PK_AlertType] PRIMARY KEY ([AlertTypeId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ApiClients' AND xtype='U')
BEGIN
    CREATE TABLE [ApiClients] (
        [ApiClientId] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ApiKeyHash] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RateLimitPerMinute] int NULL,
        CONSTRAINT [PK_ApiClients] PRIMARY KEY ([ApiClientId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Etat' AND xtype='U')
BEGIN
    CREATE TABLE [Etat] (
        [EtatAlerteId] int NOT NULL IDENTITY,
        [EtatAlerte] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Etat] PRIMARY KEY ([EtatAlerteId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PlateformeEnvoie' AND xtype='U')
BEGIN
    CREATE TABLE [PlateformeEnvoie] (
        [PlateformeId] int NOT NULL IDENTITY,
        [Plateforme] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PlateformeEnvoie] PRIMARY KEY ([PlateformeId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Statut' AND xtype='U')
BEGIN
    CREATE TABLE [Statut] (
        [StatutId] int NOT NULL IDENTITY,
        [Statut] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Statut] PRIMARY KEY ([StatutId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WebPushSubscriptions' AND xtype='U')
BEGIN
    CREATE TABLE [WebPushSubscriptions] (
        [WebPushSubscriptionId] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Endpoint] nvarchar(450) NOT NULL,
        [P256dh] nvarchar(max) NOT NULL,
        [Auth] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WebPushSubscriptions] PRIMARY KEY ([WebPushSubscriptionId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Alerte' AND xtype='U')
BEGIN
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
        CONSTRAINT [FK_Alerte_def_utilisateur_DestinataireUserId] FOREIGN KEY ([DestinataireUserId]) REFERENCES [def_utilisateur] ([util_id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alerte_def_utilisateur_ExpediteurId] FOREIGN KEY ([ExpediteurId]) REFERENCES [def_utilisateur] ([util_id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RappelSuivant' AND xtype='U')
BEGIN
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
END
GO

-- Créer les index pour les performances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_AlertGroupId')
BEGIN
    CREATE INDEX [IX_Alerte_AlertGroupId] ON [Alerte] ([AlertGroupId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_AlertTypeId')
BEGIN
    CREATE INDEX [IX_Alerte_AlertTypeId] ON [Alerte] ([AlertTypeId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_DestinataireUserId')
BEGIN
    CREATE INDEX [IX_Alerte_DestinataireUserId] ON [Alerte] ([DestinataireUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_EtatAlerteId')
BEGIN
    CREATE INDEX [IX_Alerte_EtatAlerteId] ON [Alerte] ([EtatAlerteId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_ExpediteurId')
BEGIN
    CREATE INDEX [IX_Alerte_ExpediteurId] ON [Alerte] ([ExpediteurId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_PlateformeEnvoieId')
BEGIN
    CREATE INDEX [IX_Alerte_PlateformeEnvoieId] ON [Alerte] ([PlateformeEnvoieId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_PlateformeEnvoiePlateformeId')
BEGIN
    CREATE INDEX [IX_Alerte_PlateformeEnvoiePlateformeId] ON [Alerte] ([PlateformeEnvoiePlateformeId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerte_StatutId_ProcessedByWorker_DateCreationAlerte')
BEGIN
    CREATE INDEX [IX_Alerte_StatutId_ProcessedByWorker_DateCreationAlerte] ON [Alerte] ([StatutId], [ProcessedByWorker], [DateCreationAlerte]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertProcessingQueue_AlertRecordId')
BEGIN
    CREATE INDEX [IX_AlertProcessingQueue_AlertRecordId] ON [AlertProcessingQueue] ([AlertRecordId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertProcessingQueue_Priority_QueuedAt')
BEGIN
    CREATE INDEX [IX_AlertProcessingQueue_Priority_QueuedAt] ON [AlertProcessingQueue] ([Priority], [QueuedAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RappelSuivant_AlerteId')
BEGIN
    CREATE INDEX [IX_RappelSuivant_AlerteId] ON [RappelSuivant] ([AlerteId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WebPushSubscriptions_UserId_Endpoint')
BEGIN
    CREATE UNIQUE INDEX [IX_WebPushSubscriptions_UserId_Endpoint] ON [WebPushSubscriptions] ([UserId], [Endpoint]);
END
GO

-- Insérer les données de référence
-- AlertType
IF NOT EXISTS (SELECT * FROM AlertType WHERE AlertTypeId = 1)
BEGIN
    INSERT INTO [AlertType] ([AlertType], [Description], [RequiresAcknowledgment]) VALUES 
    ('Information', 'Alerte informative - acquittement non nécessaire', 0),
    ('Obligatoire', 'Alerte obligatoire - acquittement nécessaire', 1);
END
GO

-- Statut
IF NOT EXISTS (SELECT * FROM Statut WHERE StatutId = 1)
BEGIN
    INSERT INTO [Statut] ([Statut]) VALUES 
    ('En Cours'),
    ('Envoyé'),
    ('Lu'),
    ('Échoué');
END
GO

-- Etat
IF NOT EXISTS (SELECT * FROM Etat WHERE EtatAlerteId = 1)
BEGIN
    INSERT INTO [Etat] ([EtatAlerte]) VALUES 
    ('Non Lu'),
    ('Lu');
END
GO

-- PlateformeEnvoie
IF NOT EXISTS (SELECT * FROM PlateformeEnvoie WHERE PlateformeId = 1)
BEGIN
    INSERT INTO [PlateformeEnvoie] ([Plateforme]) VALUES 
    ('Email'),
    ('WhatsApp'),
    ('Desktop');
END
GO

PRINT 'Intégration AlertSystem dans la base de données hôtel terminée avec succès !';
