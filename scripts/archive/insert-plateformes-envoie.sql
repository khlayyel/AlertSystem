-- Script pour insÃ©rer les plateformes d'envoi
-- Utilisation : sqlcmd -S "(localdb)\MSSQLLocalDB" -d "BELVEDERE_17_10_2025" -i "insert-plateformes-envoie.sql"

USE BELVEDERE_17_10_2025;
GO

PRINT 'Insertion des plateformes d''envoi...';

-- InsÃ©rer les 3 plateformes d'envoi
INSERT INTO PlateformeEnvoie (Plateforme) VALUES 
('Email'),
('WhatsApp'),
('Desktop');

-- VÃ©rifier les donnÃ©es insÃ©rÃ©es
SELECT * FROM PlateformeEnvoie ORDER BY PlateformeId;

PRINT 'Plateformes d''envoi insÃ©rÃ©es avec succÃ¨s !';
PRINT '';
PRINT 'Utilisation dans les alertes :';
PRINT '- PlateformeId 1 = Email';
PRINT '- PlateformeId 2 = WhatsApp';  
PRINT '- PlateformeId 3 = Desktop';
PRINT '';
PRINT 'Exemple d''insertion d''alerte avec plateforme :';
PRINT 'INSERT INTO Alerte (AlertTypeId, ExpedTypeId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, AppId, PlateformeEnvoieId, DestinataireId)';
PRINT 'VALUES (1, 2, ''Test'', ''Message test'', GETDATE(), 1, 2, 1, 1, 2);';

