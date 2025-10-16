-- Script simple pour ajouter la colonne WhatsAppNumber
ALTER TABLE Users ADD WhatsAppNumber NVARCHAR(20) NULL;

-- Ajouter quelques numéros de test
UPDATE Users SET WhatsAppNumber = '+21612345678' WHERE UserId = 1;
UPDATE Users SET WhatsAppNumber = '+21612345679' WHERE UserId = 2;
