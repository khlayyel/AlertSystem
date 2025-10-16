-- Script pour supprimer les doublons dans la table ExpedType
-- Garder seulement les IDs les plus bas pour chaque type

-- 1. Vérifier les doublons existants
SELECT 
    ExpedType,
    COUNT(*) as Count,
    MIN(ExpedTypeId) as MinId,
    MAX(ExpedTypeId) as MaxId
FROM ExpedType 
GROUP BY ExpedType
HAVING COUNT(*) > 1;

-- 2. Voir toutes les données avant nettoyage
SELECT * FROM ExpedType ORDER BY ExpedTypeId;

-- 3. Vérifier les références dans la table Alerte
SELECT 
    a.ExpedTypeId,
    et.ExpedType,
    COUNT(*) as AlerteCount
FROM Alerte a
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
GROUP BY a.ExpedTypeId, et.ExpedType
ORDER BY a.ExpedTypeId;

-- 4. Mettre à jour les références dans Alerte pour pointer vers les IDs les plus bas
-- Remplacer ExpedTypeId=3 (Humain dupliqué) par ExpedTypeId=1 (Humain original)
UPDATE Alerte 
SET ExpedTypeId = 1 
WHERE ExpedTypeId = 3;

-- Remplacer ExpedTypeId=4 (Service dupliqué) par ExpedTypeId=2 (Service original)
UPDATE Alerte 
SET ExpedTypeId = 2 
WHERE ExpedTypeId = 4;

-- 5. Supprimer les doublons (garder les IDs les plus bas)
DELETE FROM ExpedType WHERE ExpedTypeId = 3; -- Humain dupliqué
DELETE FROM ExpedType WHERE ExpedTypeId = 4; -- Service dupliqué

-- 6. Vérifier le résultat final
SELECT * FROM ExpedType ORDER BY ExpedTypeId;

-- 7. Vérifier que les références sont correctes
SELECT 
    a.ExpedTypeId,
    et.ExpedType,
    COUNT(*) as AlerteCount
FROM Alerte a
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
GROUP BY a.ExpedTypeId, et.ExpedType
ORDER BY a.ExpedTypeId;

PRINT 'Nettoyage des doublons ExpedType terminé avec succès !';
