-- Test final du système nettoyé - AlertePollingWorker seulement
USE AlertSystemDB;
GO

PRINT 'Testing the clean AlertePollingWorker system...';

-- Vérifier l'état du système
PRINT 'System status:';
SELECT 
    'NotificationOutbox' AS TableName,
    CASE WHEN OBJECT_ID('dbo.NotificationOutbox', 'U') IS NULL THEN 'REMOVED ✅' ELSE 'STILL EXISTS ❌' END AS Status
UNION ALL
SELECT 
    'ProcessedByWorker Column',
    CASE WHEN EXISTS(SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alerte') AND name = 'ProcessedByWorker') THEN 'EXISTS ✅' ELSE 'MISSING ❌' END
UNION ALL
SELECT 
    'Triggers Count',
    CAST(COUNT(*) AS VARCHAR) + CASE WHEN COUNT(*) = 0 THEN ' ✅' ELSE ' ❌' END
FROM sys.triggers WHERE parent_id = OBJECT_ID('dbo.Alerte');

-- Insérer une alerte test pour le nouveau système
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, 
    TitreAlerte, DescriptionAlerte, DateCreationAlerte, 
    StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId,
    ProcessedByWorker
) VALUES (
    2, 1, 2, 2, 
    'Test Clean System', 
    'Cette alerte teste le nouveau système AlertePollingWorker sans triggers ni outbox', 
    GETDATE(), 
    1, 2, NULL, NULL,
    0  -- Pas encore traité
);

-- Afficher l'alerte créée
SELECT TOP 1 
    AlerteId, 
    TitreAlerte, 
    ProcessedByWorker,
    DateCreationAlerte
FROM dbo.Alerte 
ORDER BY AlerteId DESC;

PRINT 'Test alert inserted successfully.';
PRINT 'Start AlertePollingWorker to process this alert automatically.';
PRINT 'The worker will:';
PRINT '1. Find unprocessed alerts (ProcessedByWorker = 0)';
PRINT '2. Create HistoriqueAlerte entries for each active user';
PRINT '3. Send via Email, WhatsApp, and Desktop channels';
PRINT '4. Mark ProcessedByWorker = 1';

-- Requête pour vérifier après traitement
PRINT '';
PRINT 'After processing, run these queries to verify:';
PRINT 'SELECT AlerteId, TitreAlerte, ProcessedByWorker FROM dbo.Alerte WHERE TitreAlerte LIKE ''%Clean System%'';';
PRINT 'SELECT h.AlerteId, h.DestinataireUserId, u.FullName, h.EtatAlerte FROM dbo.HistoriqueAlerte h JOIN dbo.Users u ON h.DestinataireUserId = u.UserId WHERE h.AlerteId = (SELECT AlerteId FROM dbo.Alerte WHERE TitreAlerte LIKE ''%Clean System%'');';
