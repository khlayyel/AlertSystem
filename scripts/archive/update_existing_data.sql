-- Mettre Ã  jour les donnÃ©es existantes
UPDATE AlertRecipients 
SET SendStatus = 'Sent', 
    DeliveryPlatforms = '["Email"]' 
WHERE SendStatus IS NULL OR SendStatus = '' OR DeliveryPlatforms IS NULL OR DeliveryPlatforms = '';

PRINT 'DonnÃ©es existantes mises Ã  jour avec succÃ¨s';
GO

