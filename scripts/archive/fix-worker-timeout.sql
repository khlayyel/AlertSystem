-- Fix pour éviter la boucle infinie du Worker
USE AlertSystemDB;
GO

-- Marquer toutes les alertes comme traitées pour éviter la boucle
UPDATE dbo.Alerte SET ProcessedByWorker = 1 WHERE ProcessedByWorker = 0;

-- Insérer une seule alerte test propre
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, 
    TitreAlerte, DescriptionAlerte, DateCreationAlerte, 
    StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId,
    ProcessedByWorker
) VALUES (
    2, 1, 2, 2, 
    'Test Final Clean Worker', 
    'Test unique pour vérifier le Worker sans boucle infinie', 
    GETDATE(), 
    1, 2, NULL, NULL,
    0  -- À traiter
);

-- Vérifier le résultat
SELECT TOP 1 AlerteId, TitreAlerte, ProcessedByWorker, DateCreationAlerte
FROM dbo.Alerte 
WHERE TitreAlerte LIKE '%Test Final Clean%'
ORDER BY AlerteId DESC;

PRINT 'Une seule alerte test créée. Le Worker devrait la traiter sans boucle infinie.';
