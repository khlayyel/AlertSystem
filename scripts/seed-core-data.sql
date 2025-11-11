/*
  Seed core reference data for AlerteDB
  Safe to run multiple times.
*/

IF DB_ID(N'AlerteDB') IS NULL BEGIN
  PRINT 'Database AlerteDB not found. Run create_alert_db.sql first.';
  RETURN;
END
GO

USE [AlerteDB]
GO

SET NOCOUNT ON;

-- def_App (renamed from def_Domaine)
IF NOT EXISTS (SELECT 1 FROM dbo.def_App WHERE AppId = 1)
  INSERT INTO dbo.def_App (AppId, Description) VALUES (1, N'Stock');

-- def_TypeEnvoie (renamed from def_Type)
IF NOT EXISTS (SELECT 1 FROM dbo.def_TypeEnvoie WHERE TypeEnvoieId = 1)
  INSERT INTO dbo.def_TypeEnvoie (TypeEnvoieId, Description) VALUES (1, N'Information');
IF NOT EXISTS (SELECT 1 FROM dbo.def_TypeEnvoie WHERE TypeEnvoieId = 2)
  INSERT INTO dbo.def_TypeEnvoie (TypeEnvoieId, Description) VALUES (2, N'Obligatoire');

-- def_Statut (feminine forms)
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 1)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (1, N'En Cours');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 2)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (2, N'Envoyée');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 3)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (3, N'Annulée');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Statut WHERE StatutId = 4)
  INSERT INTO dbo.def_Statut (StatutId, Description) VALUES (4, N'Échouée');

-- Update existing records to feminine forms
UPDATE dbo.def_Statut SET Description = N'Envoyée' WHERE StatutId = 2 AND Description != N'Envoyée';
UPDATE dbo.def_Statut SET Description = N'Annulée' WHERE StatutId = 3 AND Description != N'Annulée';
UPDATE dbo.def_Statut SET Description = N'Échouée' WHERE StatutId = 4 AND Description != N'Échouée';

-- def_Etat
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 1)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (1, N'Non lue');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 2)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (2, N'Lue');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 3)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (3, N'Non Confirmée');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Etat WHERE EtatId = 4)
  INSERT INTO dbo.def_Etat (EtatId, Description) VALUES (4, N'Confirmée');

-- def_PlateformeEnvoi (keep only Email and WhatsApp, remove Desktop)
IF NOT EXISTS (SELECT 1 FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 1)
  INSERT INTO dbo.def_PlateformeEnvoi (PlateformeId, Description) VALUES (1, N'Email');
IF NOT EXISTS (SELECT 1 FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 2)
  INSERT INTO dbo.def_PlateformeEnvoi (PlateformeId, Description) VALUES (2, N'WhatsApp');
-- Remove Desktop (id=3) if exists
DELETE FROM dbo.def_PlateformeEnvoi WHERE PlateformeId = 3;

-- def_TypeAlerte
IF NOT EXISTS (SELECT 1 FROM dbo.def_TypeAlerte WHERE TypeAlertId = 1)
  INSERT INTO dbo.def_TypeAlerte (TypeAlertId, AppId, Description) VALUES (1, 1, N'Rupture');

-- def_Utilisateur (with WhatsAppNumber)
IF NOT EXISTS (SELECT 1 FROM dbo.def_Utilisateur WHERE UtilisateurId = 1)
  INSERT INTO dbo.def_Utilisateur (Username, Password, AppId, Email, WhatsAppNumber) 
  VALUES (N'khalil ouerghemmi', N'123456', 1, N'khalilouerghemmi@gmail.com', N'99414008');
IF NOT EXISTS (SELECT 1 FROM dbo.def_Utilisateur WHERE UtilisateurId = 2)
  INSERT INTO dbo.def_Utilisateur (Username, Password, AppId, Email, WhatsAppNumber) 
  VALUES (N'zied soltani', N'123456', 1, N'zied.soltani11@gmail.com', N'21494064');

-- def_Alerte
IF NOT EXISTS (SELECT 1 FROM dbo.def_Alerte WHERE DefAlerteId = 1)
  INSERT INTO dbo.def_Alerte (DefTypeAlerte, ListDestinatairesId, URL, IsActive) 
  VALUES (1, N'[1,2]', N'http://localhost:5002/api/v1/stock-alerts', 1);

PRINT 'Seed completed.';
GO
