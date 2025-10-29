-- Script pour supprimer les doublons dans la table ExpedType
-- Garder seulement les IDs les plus bas pour chaque type

-- 1. VÃ©rifier les doublons existants
SELECT 
    ExpedType,
    COUNT(*) as Count,
    MIN(ExpedTypeId) as MinId,
    MAX(ExpedTypeId) as MaxId
FROM ExpedType 
GROUP BY ExpedType
HAVING COUNT(*) > 1;

-- 2. Voir toutes les donnÃ©es avant nettoyage
SELECT * FROM ExpedType ORDER BY ExpedTypeId;

-- 3. VÃ©rifier les rÃ©fÃ©rences dans la table Alerte
SELECT 
    a.ExpedTypeId,
    et.ExpedType,
    COUNT(*) as AlerteCount
FROM Alerte a
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
GROUP BY a.ExpedTypeId, et.ExpedType
ORDER BY a.ExpedTypeId;

-- 4. Mettre Ã  jour les rÃ©fÃ©rences dans Alerte pour pointer vers les IDs les plus bas
-- Remplacer ExpedTypeId=3 (Humain dupliquÃ©) par ExpedTypeId=1 (Humain original)
UPDATE Alerte 
SET ExpedTypeId = 1 
WHERE ExpedTypeId = 3;

-- Remplacer ExpedTypeId=4 (Service dupliquÃ©) par ExpedTypeId=2 (Service original)
UPDATE Alerte 
SET ExpedTypeId = 2 
WHERE ExpedTypeId = 4;

-- 5. Supprimer les doublons (garder les IDs les plus bas)
DELETE FROM ExpedType WHERE ExpedTypeId = 3; -- Humain dupliquÃ©
DELETE FROM ExpedType WHERE ExpedTypeId = 4; -- Service dupliquÃ©

-- 6. VÃ©rifier le rÃ©sultat final
SELECT * FROM ExpedType ORDER BY ExpedTypeId;

-- 7. VÃ©rifier que les rÃ©fÃ©rences sont correctes
SELECT 
    a.ExpedTypeId,
    et.ExpedType,
    COUNT(*) as AlerteCount
FROM Alerte a
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
GROUP BY a.ExpedTypeId, et.ExpedType
ORDER BY a.ExpedTypeId;

PRINT 'Nettoyage des doublons ExpedType terminÃ© avec succÃ¨s !';

