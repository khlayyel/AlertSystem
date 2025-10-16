-- Database seeding script for AlertSystem
-- Run this to ensure reference tables have required data

USE AlertSystemDB;

-- Seed AlertType table
IF NOT EXISTS (SELECT 1 FROM AlertType WHERE AlertType = 'acquittementNécessaire')
    INSERT INTO AlertType (AlertType) VALUES ('acquittementNécessaire');

IF NOT EXISTS (SELECT 1 FROM AlertType WHERE AlertType = 'acquittementNonNécessaire')
    INSERT INTO AlertType (AlertType) VALUES ('acquittementNonNécessaire');

-- Seed ExpedType table
IF NOT EXISTS (SELECT 1 FROM ExpedType WHERE ExpedType = 'Humain')
    INSERT INTO ExpedType (ExpedType) VALUES ('Humain');

IF NOT EXISTS (SELECT 1 FROM ExpedType WHERE ExpedType = 'Service')
    INSERT INTO ExpedType (ExpedType) VALUES ('Service');

-- Seed Statut table
IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'En Cours')
    INSERT INTO Statut (Statut) VALUES ('En Cours');

IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'Terminé')
    INSERT INTO Statut (Statut) VALUES ('Terminé');

IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'Échoué')
    INSERT INTO Statut (Statut) VALUES ('Échoué');

-- Seed Etat table
IF NOT EXISTS (SELECT 1 FROM Etat WHERE EtatAlerte = 'Non Lu')
    INSERT INTO Etat (EtatAlerte) VALUES ('Non Lu');

IF NOT EXISTS (SELECT 1 FROM Etat WHERE EtatAlerte = 'Lu')
    INSERT INTO Etat (EtatAlerte) VALUES ('Lu');

PRINT 'Database seeding completed successfully!';
