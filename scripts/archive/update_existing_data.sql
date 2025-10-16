-- Mettre à jour les données existantes
UPDATE AlertRecipients 
SET SendStatus = 'Sent', 
    DeliveryPlatforms = '["Email"]' 
WHERE SendStatus IS NULL OR SendStatus = '' OR DeliveryPlatforms IS NULL OR DeliveryPlatforms = '';

PRINT 'Données existantes mises à jour avec succès';
GO
