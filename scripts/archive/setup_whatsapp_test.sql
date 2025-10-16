-- Script pour configurer WhatsApp dans la base de données
-- Ajouter la colonne WhatsAppNumber si elle n'existe pas
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'WhatsAppNumber')
BEGIN
    ALTER TABLE Users ADD WhatsAppNumber NVARCHAR(20) NULL;
    PRINT 'Colonne WhatsAppNumber ajoutée avec succès';
END
ELSE
BEGIN
    PRINT 'Colonne WhatsAppNumber existe déjà';
END

-- Ajouter des numéros de test pour les utilisateurs existants
-- Remplacez ces numéros par de vrais numéros pour tester
UPDATE Users SET WhatsAppNumber = '+21612345678' WHERE UserId = 1;
UPDATE Users SET WhatsAppNumber = '+21698765432' WHERE UserId = 2;

-- Afficher les utilisateurs avec leurs numéros WhatsApp
SELECT UserId, Username, Email, PhoneNumber, WhatsAppNumber 
FROM Users 
ORDER BY UserId;

PRINT 'Configuration WhatsApp terminée. Utilisez de vrais numéros pour tester l''envoi.';
