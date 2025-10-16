-- Script pour ajouter les nouvelles colonnes au système de reminders
USE [AlertSystemDb]
GO

-- Vérifier si les colonnes existent déjà
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'DeliveryPlatforms')
BEGIN
    ALTER TABLE AlertRecipients ADD DeliveryPlatforms NVARCHAR(MAX) NOT NULL DEFAULT '[]'
    PRINT 'Colonne DeliveryPlatforms ajoutée'
END
ELSE
BEGIN
    PRINT 'Colonne DeliveryPlatforms existe déjà'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'SendStatus')
BEGIN
    ALTER TABLE AlertRecipients ADD SendStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending'
    PRINT 'Colonne SendStatus ajoutée'
END
ELSE
BEGIN
    PRINT 'Colonne SendStatus existe déjà'
END

-- Créer les index pour les performances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertRecipients_SendStatus_NextReminderAt')
BEGIN
    CREATE INDEX IX_AlertRecipients_SendStatus_NextReminderAt ON AlertRecipients (SendStatus, NextReminderAt)
    PRINT 'Index SendStatus_NextReminderAt créé'
END

-- Mettre à jour les données existantes (seulement si les colonnes existent)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'DeliveryPlatforms')
BEGIN
    UPDATE AlertRecipients 
    SET DeliveryPlatforms = '["Email"]', 
        SendStatus = CASE 
            WHEN IsConfirmed = 1 THEN 'Sent' 
            ELSE 'Pending' 
        END
    WHERE DeliveryPlatforms = '[]' OR SendStatus = 'Pending'
    PRINT 'Données existantes mises à jour'
END

PRINT 'Colonnes ajoutées et données mises à jour avec succès !'
GO
