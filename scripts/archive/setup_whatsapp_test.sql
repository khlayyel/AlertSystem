-- Script pour configurer WhatsApp dans la base de donnÃ©es
-- Ajouter la colonne WhatsAppNumber si elle n'existe pas
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'WhatsAppNumber')
BEGIN
    ALTER TABLE Users ADD WhatsAppNumber NVARCHAR(20) NULL;
    PRINT 'Colonne WhatsAppNumber ajoutÃ©e avec succÃ¨s';
END
ELSE
BEGIN
    PRINT 'Colonne WhatsAppNumber existe dÃ©jÃ ';
END

-- Ajouter des numÃ©ros de test pour les utilisateurs existants
-- Remplacez ces numÃ©ros par de vrais numÃ©ros pour tester
UPDATE Users SET WhatsAppNumber = '+21612345678' WHERE UserId = 1;
UPDATE Users SET WhatsAppNumber = '+21698765432' WHERE UserId = 2;

-- Afficher les utilisateurs avec leurs numÃ©ros WhatsApp
SELECT UserId, Username, Email, PhoneNumber, WhatsAppNumber 
FROM Users 
ORDER BY UserId;

PRINT 'Configuration WhatsApp terminÃ©e. Utilisez de vrais numÃ©ros pour tester l''envoi.';

