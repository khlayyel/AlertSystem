-- Script pour ajouter les nouvelles colonnes au systÃ¨me de reminders
USE [BELVEDERE_17_10_2025]
GO

-- VÃ©rifier si les colonnes existent dÃ©jÃ 
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'DeliveryPlatforms')
BEGIN
    ALTER TABLE AlertRecipients ADD DeliveryPlatforms NVARCHAR(MAX) NOT NULL DEFAULT '[]'
    PRINT 'Colonne DeliveryPlatforms ajoutÃ©e'
END
ELSE
BEGIN
    PRINT 'Colonne DeliveryPlatforms existe dÃ©jÃ '
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'SendStatus')
BEGIN
    ALTER TABLE AlertRecipients ADD SendStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending'
    PRINT 'Colonne SendStatus ajoutÃ©e'
END
ELSE
BEGIN
    PRINT 'Colonne SendStatus existe dÃ©jÃ '
END

-- CrÃ©er les index pour les performances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertRecipients_SendStatus_NextReminderAt')
BEGIN
    CREATE INDEX IX_AlertRecipients_SendStatus_NextReminderAt ON AlertRecipients (SendStatus, NextReminderAt)
    PRINT 'Index SendStatus_NextReminderAt crÃ©Ã©'
END

-- Mettre Ã  jour les donnÃ©es existantes (seulement si les colonnes existent)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AlertRecipients' AND COLUMN_NAME = 'DeliveryPlatforms')
BEGIN
    UPDATE AlertRecipients 
    SET DeliveryPlatforms = '["Email"]', 
        SendStatus = CASE 
            WHEN IsConfirmed = 1 THEN 'Sent' 
            ELSE 'Pending' 
        END
    WHERE DeliveryPlatforms = '[]' OR SendStatus = 'Pending'
    PRINT 'DonnÃ©es existantes mises Ã  jour'
END

PRINT 'Colonnes ajoutÃ©es et donnÃ©es mises Ã  jour avec succÃ¨s !'
GO

