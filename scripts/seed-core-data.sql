/*
  Seed core reference data for AlertDB
  Safe to run multiple times.
*/

IF DB_ID(N'AlertDB') IS NULL BEGIN
  PRINT 'Database AlertDB not found. Run create_alert_db.sql first.';
  RETURN;
END
GO

USE [AlertDB]
GO

SET NOCOUNT ON;

-- def_Domaine
IF NOT EXISTS (SELECT 1 FROM dbo.def_Domaine WHERE DomaineId = 1)
  INSERT INTO dbo.def_Domaine (DomaineId, Description) VALUES (1, N'Stock');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Domaine WHERE DomaineId = 2)
  INSERT INTO dbo.def_Domaine (DomaineId, Description) VALUES (2, N'GRH');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Domaine WHERE DomaineId = 3)
  INSERT INTO dbo.def_Domaine (DomaineId, Description) VALUES (3, N'BNQ');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Domaine WHERE DomaineId = 4)
  INSERT INTO dbo.def_Domaine (DomaineId, Description) VALUES (4, N'Réservation');

-- def_Type
IF NOT EXISTS (SELECT 1 FROM dbo.def_Type WHERE TypeId = 1)
  INSERT INTO dbo.def_Type (TypeId, Description) VALUES (1, N'Information');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Type WHERE TypeId = 2)
  INSERT INTO dbo.def_Type (TypeId, Description) VALUES (2, N'Obligatoire');

-- def_Statut
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 1)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (1, N'En Cours');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 2)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (2, N'Envoyé');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 3)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (3, N'Annulé');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 4)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (4, N'Échoué');

-- def_Etat
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 1)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (1, N'Non Lu');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 2)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (2, N'Lu');

-- Additional states for mandatory (obligatoire) alerts
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 3)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (3, N'Non Confirmée');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 4)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (4, N'Confirmée');

-- def_PlateformeEnvoi
IF NOT EXISTS (SELECT 1 FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 1)
  INSERT INTO dbo.def_PlateformeEnvoi (PlateformeId, Description) VALUES (1, N'Email');
IF NOT EXISTS (SELECT 1 FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 2)
  INSERT INTO dbo.def_PlateformeEnvoi (PlateformeId, Description) VALUES (2, N'WhatsApp');
IF NOT EXISTS (SELECT 1 FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 3)
  INSERT INTO dbo.def_PlateformeEnvoi (PlateformeId, Description) VALUES (3, N'Desktop');

PRINT 'Seed completed.';
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM def_Statut)
BEGIN
    INSERT INTO def_Statut (StatutId, Description) VALUES (1,'En cours'),(2,'Envoyé'),(3,'Annulé'),(4,'Echoué');
END

IF NOT EXISTS (SELECT 1 FROM def_Etat)
BEGIN
    INSERT INTO def_Etat (EtatId, Description) VALUES (1,'Non Lu'),(2,'Lu'),(3,'Non Confirmée'),(4,'Confirmée');;
END

IF NOT EXISTS (SELECT 1 FROM def_Type)
BEGIN
    INSERT INTO def_Type (TypeId, Description) VALUES (1,'Information'),(2,'Obligatoire');
END

IF NOT EXISTS (SELECT 1 FROM def_PlateformeEnvoi)
BEGIN
    INSERT INTO def_PlateformeEnvoi (PlateformeId, Description) VALUES (1,'Email'),(2,'WhatsApp'),(3,'Desktop');
END



