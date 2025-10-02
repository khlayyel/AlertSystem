-- Ajouter la colonne WhatsAppNumber à la table Users
USE AlertSystemDb;
GO

-- Vérifier si la colonne n'existe pas déjà
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'WhatsAppNumber')
BEGIN
    ALTER TABLE Users ADD WhatsAppNumber NVARCHAR(20) NULL;
    PRINT 'Colonne WhatsAppNumber ajoutée avec succès à la table Users';
END
ELSE
BEGIN
    PRINT 'La colonne WhatsAppNumber existe déjà dans la table Users';
END
GO

