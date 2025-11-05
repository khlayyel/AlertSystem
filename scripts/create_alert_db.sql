/*
  AlertSystem - Create database and core tables
  Idempotent script: safe to run multiple times.
*/

-- 1) Create database if not exists
IF DB_ID(N'AlertDB') IS NULL
BEGIN
  PRINT 'Creating database AlertDB...';
  DECLARE @sql nvarchar(max) = N'CREATE DATABASE [AlertDB]';
  EXEC(@sql);
END
GO

USE [AlertDB]
GO

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
GO

-- 2) Reference tables -------------------------------------------------------

IF OBJECT_ID(N'dbo.def_Domaine', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Domaine (
    DomaineId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

IF OBJECT_ID(N'dbo.def_Type', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Type (
    TypeId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

IF OBJECT_ID(N'dbo.def_Statut', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Statut (
    StatutId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

IF OBJECT_ID(N'dbo.def_Etat', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_Etat (
    EtatId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

IF OBJECT_ID(N'dbo.def_PlateformeEnvoi', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.def_PlateformeEnvoi (
    PlateformeId int NOT NULL PRIMARY KEY,
    Description nvarchar(100) NOT NULL
  );
END
GO

-- 3) Main table: Alerte -----------------------------------------------------

IF OBJECT_ID(N'dbo.Alerte', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.Alerte (
    AlertRecordId       bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AlertGroupId        uniqueidentifier      NOT NULL,
    DomaineId           int                   NOT NULL,
    TypeId              int                   NOT NULL,
    TitreAlerte         nvarchar(255)         NOT NULL,
    DescriptionAlerte   nvarchar(max)         NULL,
    DateCreationAlerte  datetime2             NOT NULL CONSTRAINT DF_Alerte_DateCreation DEFAULT (SYSUTCDATETIME()),
    StatutId            int                   NOT NULL CONSTRAINT DF_Alerte_Statut DEFAULT (1),
    EtatId              int                   NOT NULL CONSTRAINT DF_Alerte_Etat DEFAULT (1),
    PlateformeEnvoieId  int                   NOT NULL,
    Destinataire        nvarchar(255)         NOT NULL, -- email ou téléphone
    DateLecture         datetime2             NULL,
    RappelSuivant       datetime2             NULL,
    ProcessedByWorker   bit                   NOT NULL CONSTRAINT DF_Alerte_Processed DEFAULT (0),
    AttemptCount        int                   NOT NULL CONSTRAINT DF_Alerte_Attempts DEFAULT (0)
  );

  -- Foreign keys
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Domaine     FOREIGN KEY (DomaineId)          REFERENCES dbo.def_Domaine(DomaineId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Type        FOREIGN KEY (TypeId)             REFERENCES dbo.def_Type(TypeId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Statut      FOREIGN KEY (StatutId)           REFERENCES dbo.def_Statut(StatutId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Etat        FOREIGN KEY (EtatId)             REFERENCES dbo.def_Etat(EtatId);
  ALTER TABLE dbo.Alerte ADD CONSTRAINT FK_Alerte_Plateforme  FOREIGN KEY (PlateformeEnvoieId) REFERENCES dbo.def_PlateformeEnvoi(PlateformeId);

  -- Helpful indexes
  CREATE INDEX IX_Alerte_Processed ON dbo.Alerte(ProcessedByWorker, StatutId, AttemptCount);
  CREATE INDEX IX_Alerte_DomaineDate ON dbo.Alerte(DomaineId, DateCreationAlerte DESC);
  CREATE INDEX IX_Alerte_Group ON dbo.Alerte(AlertGroupId);
END
GO

PRINT 'AlertDB schema is ready.'
GO


