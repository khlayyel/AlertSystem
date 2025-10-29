-- Fix pour Ã©viter la boucle infinie du Worker
USE BELVEDERE_17_10_2025;
GO

-- Marquer toutes les alertes comme traitÃ©es pour Ã©viter la boucle
UPDATE dbo.Alerte SET ProcessedByWorker = 1 WHERE ProcessedByWorker = 0;

-- InsÃ©rer une seule alerte test propre
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, 
    TitreAlerte, DescriptionAlerte, DateCreationAlerte, 
    StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId,
    ProcessedByWorker
) VALUES (
    2, 1, 2, 2, 
    'Test Final Clean Worker', 
    'Test unique pour vÃ©rifier le Worker sans boucle infinie', 
    GETDATE(), 
    1, 2, NULL, NULL,
    0  -- Ã€ traiter
);

-- VÃ©rifier le rÃ©sultat
SELECT TOP 1 AlerteId, TitreAlerte, ProcessedByWorker, DateCreationAlerte
FROM dbo.Alerte 
WHERE TitreAlerte LIKE '%Test Final Clean%'
ORDER BY AlerteId DESC;

PRINT 'Une seule alerte test crÃ©Ã©e. Le Worker devrait la traiter sans boucle infinie.';

