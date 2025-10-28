-- Script to clean lookup data and remove special characters
-- Run this AFTER applying the migration to the hotel database

USE BELVEDERE_17_10_2025;
GO

-- Clean Statut table (remove accents and special characters)
UPDATE dbo.Statut SET Statut = 'EnCours' WHERE StatutId = 1;
UPDATE dbo.Statut SET Statut = 'Envoye' WHERE StatutId = 2;
UPDATE dbo.Statut SET Statut = 'Annule' WHERE StatutId = 3;
UPDATE dbo.Statut SET Statut = 'Echoue' WHERE StatutId = 4;

-- Clean Etat table
UPDATE dbo.Etat SET Etat = 'Lu' WHERE EtatAlerteId = 1;
UPDATE dbo.Etat SET Etat = 'NonLu' WHERE EtatAlerteId = 2;

-- Clean AlertType table
UPDATE dbo.AlertType SET AlertType = 'acquittementNecessaire' WHERE AlertTypeId = 1;
UPDATE dbo.AlertType SET AlertType = 'acquittementNonNecessaire' WHERE AlertTypeId = 2;

-- If there are special characters in PlateformeEnvoie, clean them too
UPDATE dbo.PlateformeEnvoie SET Plateforme = 'Email' WHERE PlateformeId = 1;
UPDATE dbo.PlateformeEnvoie SET Plateforme = 'SMS' WHERE PlateformeId = 2;
UPDATE dbo.PlateformeEnvoie SET Plateforme = 'WhatsApp' WHERE PlateformeId = 3;
UPDATE dbo.PlateformeEnvoie SET Plateforme = 'Desktop' WHERE PlateformeId = 4;

PRINT 'Lookup data cleaned successfully - no special characters remaining';
GO

