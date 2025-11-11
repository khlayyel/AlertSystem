/*
  AlertSystem - Create database and core tables
  Idempotent script: safe to run multiple times.
  Usage: replace the value of @DB_NAME before running if you want another database name
*/

-- 0) Choose database name here
DECLARE @DB_NAME sysname = N'AlerteDB';  -- change me (e.g., AlertDB_Prod_2025)

-- 1) Create database if not exists
IF DB_ID(@DB_NAME) IS NULL
BEGIN
  PRINT 'Creating database ' + @DB_NAME + ' ...';
  DECLARE @sqlCreate nvarchar(max) = N'CREATE DATABASE [' + @DB_NAME + N']';
  EXEC(@sqlCreate);
END
GO

-- Switch DB context explicitly (non-dynamic so it persists across batches)
USE [AlerteDB];
GO

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
GO

-- 2) Reference tables -------------------------------------------------------

-- def_App (renamed from def_Domaine)
IF OBJECT_ID(N'dbo.def_App', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_App (
    AppId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- def_TypeEnvoie (renamed from def_Type)
IF OBJECT_ID(N'dbo.def_TypeEnvoie', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_TypeEnvoie (
    TypeEnvoieId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- def_Statut
IF OBJECT_ID(N'dbo.def_Statut', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Statut (
    StatutId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- def_Etat
IF OBJECT_ID(N'dbo.def_Etat', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Etat (
    EtatId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- def_PlateformeEnvoi (keep only Email and WhatsApp)
IF OBJECT_ID(N'dbo.def_PlateformeEnvoi', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_PlateformeEnvoi (
    PlateformeId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- def_TypeAlerte (new table)
IF OBJECT_ID(N'dbo.def_TypeAlerte', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_TypeAlerte (
    TypeAlertId int NOT NULL PRIMARY KEY,
    AppId int NOT NULL,
    Description nvarchar(100) NOT NULL,
    CONSTRAINT FK_TypeAlerte_App FOREIGN KEY (AppId) REFERENCES dbo.def_App(AppId)
  );
END
GO

-- def_Utilisateur (new table with WhatsAppNumber)
IF OBJECT_ID(N'dbo.def_Utilisateur', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Utilisateur (
    UtilisateurId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username nvarchar(100) NOT NULL,
    Password nvarchar(255) NOT NULL,
    AppId int NOT NULL,
    Email nvarchar(255) NOT NULL,
    WhatsAppNumber nvarchar(20) NULL,
    CONSTRAINT FK_Utilisateur_App FOREIGN KEY (AppId) REFERENCES dbo.def_App(AppId),
    CONSTRAINT UQ_Utilisateur_Email UNIQUE (Email)
  );
  CREATE INDEX IX_Utilisateur_WhatsAppNumber ON dbo.def_Utilisateur(WhatsAppNumber);
END
GO

-- def_Alerte (new table)
IF OBJECT_ID(N'dbo.def_Alerte', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Alerte (
    DefAlerteId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DefTypeAlerte int NOT NULL,
    ListDestinatairesId nvarchar(max) NOT NULL, -- JSON array: [1,2,3]
    URL nvarchar(500) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DefAlerte_TypeAlerte FOREIGN KEY (DefTypeAlerte) REFERENCES dbo.def_TypeAlerte(TypeAlertId)
  );
  CREATE INDEX IX_DefAlerte_IsActive ON dbo.def_Alerte(IsActive);
  CREATE INDEX IX_DefAlerte_TypeAlerte ON dbo.def_Alerte(DefTypeAlerte);
END
GO

-- 3) Main table: Alerte -----------------------------------------------------

IF OBJECT_ID(N'dbo.Alerte', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.Alerte (
    AlertRecordId       bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AlertGroupId        uniqueidentifier      NOT NULL,
    AppId               int                   NOT NULL,
    TypeEnvoieId        int                   NOT NULL,
    TitreAlerte         nvarchar(255)         NOT NULL,
    DescriptionAlerte   nvarchar(max)         NULL,
    DateCreationAlerte  datetime2             NOT NULL CONSTRAINT DF_Alerte_DateCreation DEFAULT (SYSUTCDATETIME()),
    StatutId            int                   NOT NULL CONSTRAINT DF_Alerte_Statut DEFAULT (1),
    EtatId              int                   NOT NULL CONSTRAINT DF_Alerte_Etat DEFAULT (1),
    PlateformeEnvoieId  int                   NOT NULL,
    Destinataire        nvarchar(255)         NOT NULL,
    DateLecture         datetime2             NULL,
    RappelSuivant       datetime2             NULL,
    ProcessedByWorker   bit                   NOT NULL CONSTRAINT DF_Alerte_Processed DEFAULT (0),
    AttemptCount        int                   NOT NULL CONSTRAINT DF_Alerte_Attempts DEFAULT (0)
  );

  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_App            FOREIGN KEY (AppId)            REFERENCES dbo.def_App(AppId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_TypeEnvoie     FOREIGN KEY (TypeEnvoieId)     REFERENCES dbo.def_TypeEnvoie(TypeEnvoieId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Statut         FOREIGN KEY (StatutId)         REFERENCES dbo.def_Statut(StatutId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Etat           FOREIGN KEY (EtatId)           REFERENCES dbo.def_Etat(EtatId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Plateforme     FOREIGN KEY (PlateformeEnvoieId) REFERENCES dbo.def_PlateformeEnvoi(PlateformeId);

  CREATE INDEX IX_Alerte_Processed ON dbo.Alerte(ProcessedByWorker, StatutId, AttemptCount);
  CREATE INDEX IX_Alerte_AppDate ON dbo.Alerte(AppId, DateCreationAlerte DESC);
  CREATE INDEX IX_Alerte_Group ON dbo.Alerte(AlertGroupId);
END
GO

PRINT 'Schema is ready for database.'
GO
