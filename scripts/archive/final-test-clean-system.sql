-- Test final du systÃ¨me nettoyÃ© - AlertePollingWorker seulement
USE BELVEDERE_17_10_2025;
GO

PRINT 'Testing the clean AlertePollingWorker system...';

-- VÃ©rifier l'Ã©tat du systÃ¨me
PRINT 'System status:';
SELECT 
    'NotificationOutbox' AS TableName,
    CASE WHEN OBJECT_ID('dbo.NotificationOutbox', 'U') IS NULL THEN 'REMOVED âœ…' ELSE 'STILL EXISTS âŒ' END AS Status
UNION ALL
SELECT 
    'ProcessedByWorker Column',
    CASE WHEN EXISTS(SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alerte') AND name = 'ProcessedByWorker') THEN 'EXISTS âœ…' ELSE 'MISSING âŒ' END
UNION ALL
SELECT 
    'Triggers Count',
    CAST(COUNT(*) AS VARCHAR) + CASE WHEN COUNT(*) = 0 THEN ' âœ…' ELSE ' âŒ' END
FROM sys.triggers WHERE parent_id = OBJECT_ID('dbo.Alerte');

-- InsÃ©rer une alerte test pour le nouveau systÃ¨me
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, 
    TitreAlerte, DescriptionAlerte, DateCreationAlerte, 
    StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId,
    ProcessedByWorker
) VALUES (
    2, 1, 2, 2, 
    'Test Clean System', 
    'Cette alerte teste le nouveau systÃ¨me AlertePollingWorker sans triggers ni outbox', 
    GETDATE(), 
    1, 2, NULL, NULL,
    0  -- Pas encore traitÃ©
);

-- Afficher l'alerte crÃ©Ã©e
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

-- RequÃªte pour vÃ©rifier aprÃ¨s traitement
PRINT '';
PRINT 'After processing, run these queries to verify:';
PRINT 'SELECT AlerteId, TitreAlerte, ProcessedByWorker FROM dbo.Alerte WHERE TitreAlerte LIKE ''%Clean System%'';';
PRINT 'SELECT h.AlerteId, h.DestinataireUserId, u.FullName, h.EtatAlerte FROM dbo.HistoriqueAlerte h JOIN dbo.Users u ON h.DestinataireUserId = u.UserId WHERE h.AlerteId = (SELECT AlerteId FROM dbo.Alerte WHERE TitreAlerte LIKE ''%Clean System%'');';

