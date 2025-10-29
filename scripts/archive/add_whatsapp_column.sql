-- Ajouter la colonne WhatsAppNumber Ã  la table Users
USE BELVEDERE_17_10_2025;
GO

-- VÃ©rifier si la colonne n'existe pas dÃ©jÃ 
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'WhatsAppNumber')
BEGIN
    ALTER TABLE Users ADD WhatsAppNumber NVARCHAR(20) NULL;
    PRINT 'Colonne WhatsAppNumber ajoutÃ©e avec succÃ¨s Ã  la table Users';
END
ELSE
BEGIN
    PRINT 'La colonne WhatsAppNumber existe dÃ©jÃ  dans la table Users';
END
GO


