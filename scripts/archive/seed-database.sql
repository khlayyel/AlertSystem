-- Database seeding script for AlertSystem
-- Run this to ensure reference tables have required data

USE BELVEDERE_17_10_2025;

-- Seed AlertType table
IF NOT EXISTS (SELECT 1 FROM AlertType WHERE AlertType = 'acquittementNÃ©cessaire')
    INSERT INTO AlertType (AlertType) VALUES ('acquittementNÃ©cessaire');

IF NOT EXISTS (SELECT 1 FROM AlertType WHERE AlertType = 'acquittementNonNÃ©cessaire')
    INSERT INTO AlertType (AlertType) VALUES ('acquittementNonNÃ©cessaire');

-- Seed ExpedType table
IF NOT EXISTS (SELECT 1 FROM ExpedType WHERE ExpedType = 'Humain')
    INSERT INTO ExpedType (ExpedType) VALUES ('Humain');

IF NOT EXISTS (SELECT 1 FROM ExpedType WHERE ExpedType = 'Service')
    INSERT INTO ExpedType (ExpedType) VALUES ('Service');

-- Seed Statut table
IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'En Cours')
    INSERT INTO Statut (Statut) VALUES ('En Cours');

IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'TerminÃ©')
    INSERT INTO Statut (Statut) VALUES ('TerminÃ©');

IF NOT EXISTS (SELECT 1 FROM Statut WHERE Statut = 'Ã‰chouÃ©')
    INSERT INTO Statut (Statut) VALUES ('Ã‰chouÃ©');

-- Seed Etat table
IF NOT EXISTS (SELECT 1 FROM Etat WHERE EtatAlerte = 'Non Lu')
    INSERT INTO Etat (EtatAlerte) VALUES ('Non Lu');

IF NOT EXISTS (SELECT 1 FROM Etat WHERE EtatAlerte = 'Lu')
    INSERT INTO Etat (EtatAlerte) VALUES ('Lu');

PRINT 'Database seeding completed successfully!';

