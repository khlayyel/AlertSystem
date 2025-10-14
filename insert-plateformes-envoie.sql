-- Script pour insérer les plateformes d'envoi
-- Utilisation : sqlcmd -S "(localdb)\MSSQLLocalDB" -d "AlertSystemDB" -i "insert-plateformes-envoie.sql"

USE AlertSystemDB;
GO

PRINT 'Insertion des plateformes d''envoi...';

-- Insérer les 3 plateformes d'envoi
INSERT INTO PlateformeEnvoie (Plateforme) VALUES 
('Email'),
('WhatsApp'),
('Desktop');

-- Vérifier les données insérées
SELECT * FROM PlateformeEnvoie ORDER BY PlateformeId;

PRINT 'Plateformes d''envoi insérées avec succès !';
PRINT '';
PRINT 'Utilisation dans les alertes :';
PRINT '- PlateformeId 1 = Email';
PRINT '- PlateformeId 2 = WhatsApp';  
PRINT '- PlateformeId 3 = Desktop';
PRINT '';
PRINT 'Exemple d''insertion d''alerte avec plateforme :';
PRINT 'INSERT INTO Alerte (AlertTypeId, ExpedTypeId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, AppId, PlateformeEnvoieId, DestinataireId)';
PRINT 'VALUES (1, 2, ''Test'', ''Message test'', GETDATE(), 1, 2, 1, 1, 2);';
