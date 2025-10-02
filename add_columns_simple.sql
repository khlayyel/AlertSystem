-- Script simple pour ajouter les colonnes
USE [AlertSystemDB]
GO

-- Ajouter DeliveryPlatforms
ALTER TABLE AlertRecipients ADD DeliveryPlatforms NVARCHAR(MAX) NOT NULL DEFAULT '[]'
GO

-- Ajouter SendStatus  
ALTER TABLE AlertRecipients ADD SendStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending'
GO

-- Créer l'index
CREATE INDEX IX_AlertRecipients_SendStatus_NextReminderAt ON AlertRecipients (SendStatus, NextReminderAt)
GO

-- Mettre à jour les données existantes
UPDATE AlertRecipients 
SET DeliveryPlatforms = '["Email"]', 
    SendStatus = CASE 
        WHEN IsConfirmed = 1 THEN 'Sent' 
        ELSE 'Pending' 
    END
GO

PRINT 'Colonnes ajoutées avec succès !'
GO
