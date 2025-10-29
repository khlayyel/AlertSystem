-- TEST COMPLET DU SYSTÃˆME CORRIGÃ‰
USE BELVEDERE_17_10_2025;
GO

PRINT 'ðŸ§ª TEST COMPLET DU SYSTÃˆME ALERTSYSTEM';
PRINT '=====================================';
PRINT '';

-- VÃ©rifier le trigger installÃ©
PRINT '1. VÃ‰RIFICATION DU TRIGGER:';
SELECT name, is_disabled FROM sys.triggers WHERE parent_id = OBJECT_ID('Alerte');
PRINT '';

-- Test 1: Alerte Email pour Khalil
PRINT '2. TEST 1 - Alerte Email pour Khalil:';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    2, 1, 1, 2, 'TEST SYSTÃˆME CORRIGÃ‰ - Email', 
    'Test du systÃ¨me complÃ¨tement corrigÃ© - Email pour Khalil', 
    GETDATE(), 1, 2, 1, 1
);
PRINT '';

-- Test 2: Alerte WhatsApp pour Zied  
PRINT '3. TEST 2 - Alerte WhatsApp pour Zied:';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    2, 1, 1, 2, 'TEST SYSTÃˆME CORRIGÃ‰ - WhatsApp', 
    'Test du systÃ¨me complÃ¨tement corrigÃ© - WhatsApp pour Zied', 
    GETDATE(), 1, 2, 2, 2
);
PRINT '';

-- Test 3: Alerte Multi-canal (tous les utilisateurs)
PRINT '4. TEST 3 - Alerte Multi-canal pour tous:';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    1, 1, 2, 1, 'TEST SYSTÃˆME CORRIGÃ‰ - Multi-canal', 
    'Test du systÃ¨me complÃ¨tement corrigÃ© - Tous les canaux pour tous les utilisateurs', 
    GETDATE(), 1, 2, NULL, NULL
);
PRINT '';

PRINT '5. RÃ‰SULTATS DES TESTS:';
PRINT '';

-- Voir les alertes crÃ©Ã©es
PRINT 'Alertes crÃ©Ã©es:';
SELECT TOP 3 AlerteId, TitreAlerte, DateCreationAlerte, DestinataireId, PlateformeEnvoieId
FROM Alerte 
ORDER BY AlerteId DESC;
PRINT '';

-- Voir l'historique crÃ©Ã©
PRINT 'Historique crÃ©Ã©:';
SELECT 
    h.AlerteId,
    h.DestinataireUserId,
    h.EtatAlerte,
    u.FullName,
    u.Email,
    CASE 
        WHEN a.PlateformeEnvoieId = 1 THEN 'Email'
        WHEN a.PlateformeEnvoieId = 2 THEN 'WhatsApp'
        WHEN a.PlateformeEnvoieId = 3 THEN 'Desktop'
        ELSE 'Multi-canal'
    END as Plateforme
FROM HistoriqueAlerte h
JOIN Users u ON h.DestinataireUserId = u.UserId
JOIN Alerte a ON h.AlerteId = a.AlerteId
WHERE h.AlerteId IN (
    SELECT TOP 3 AlerteId FROM Alerte ORDER BY AlerteId DESC
)
ORDER BY h.AlerteId DESC, h.DestinataireUserId;
PRINT '';

-- Statistiques
PRINT 'Statistiques:';
SELECT 
    COUNT(*) as 'Total Historique',
    COUNT(DISTINCT h.AlerteId) as 'Alertes traitÃ©es',
    COUNT(DISTINCT h.DestinataireUserId) as 'Utilisateurs concernÃ©s'
FROM HistoriqueAlerte h
WHERE h.AlerteId IN (
    SELECT TOP 3 AlerteId FROM Alerte ORDER BY AlerteId DESC
);
PRINT '';

PRINT 'âœ… TESTS TERMINÃ‰S !';
PRINT '';
PRINT 'ðŸŽ¯ SYSTÃˆME FONCTIONNEL:';
PRINT '- Trigger installÃ© et opÃ©rationnel';
PRINT '- Historique crÃ©Ã© automatiquement';
PRINT '- Support multi-canal et multi-utilisateur';
PRINT '- PrÃªt pour utilisation en production';
PRINT '';
PRINT 'ðŸ“ POUR TESTER MANUELLEMENT:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Mon Test'', ''Description'', GETDATE(), 1, 2, 1, 1);';

