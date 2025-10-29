-- Script complet pour nettoyer tous les doublons dans les tables de rÃ©fÃ©rence

PRINT 'DÃ©but du nettoyage des doublons dans toutes les tables de rÃ©fÃ©rence...';

-- ========================================
-- 1. NETTOYAGE AlertType
-- ========================================
PRINT 'Nettoyage AlertType...';

-- Mettre Ã  jour les rÃ©fÃ©rences dans Alerte
UPDATE Alerte SET AlertTypeId = 1 WHERE AlertTypeId = 3; -- acquittementNÃ©cessaire
UPDATE Alerte SET AlertTypeId = 2 WHERE AlertTypeId = 4; -- acquittementNonNÃ©cessaire

-- Supprimer les doublons
DELETE FROM AlertType WHERE AlertTypeId IN (3, 4);

-- ========================================
-- 2. NETTOYAGE Statut
-- ========================================
PRINT 'Nettoyage Statut...';

-- Mettre Ã  jour les rÃ©fÃ©rences dans Alerte
UPDATE Alerte SET StatutId = 1 WHERE StatutId = 5; -- En Cours
UPDATE Alerte SET StatutId = 4 WHERE StatutId = 7; -- Ã‰chouÃ©

-- Supprimer les doublons
DELETE FROM Statut WHERE StatutId IN (5, 7);

-- ========================================
-- 3. NETTOYAGE Etat
-- ========================================
PRINT 'Nettoyage Etat...';

-- Mettre Ã  jour les rÃ©fÃ©rences dans Alerte
UPDATE Alerte SET EtatAlerteId = 2 WHERE EtatAlerteId = 3; -- Non Lu
UPDATE Alerte SET EtatAlerteId = 1 WHERE EtatAlerteId = 4; -- Lu

-- Supprimer les doublons
DELETE FROM Etat WHERE EtatAlerteId IN (3, 4);

-- ========================================
-- VÃ‰RIFICATION FINALE
-- ========================================
PRINT 'VÃ©rification finale...';

PRINT 'AlertType final :';
SELECT AlertTypeId, AlertType FROM AlertType ORDER BY AlertTypeId;

PRINT 'ExpedType final :';
SELECT ExpedTypeId, ExpedType FROM ExpedType ORDER BY ExpedTypeId;

PRINT 'Statut final :';
SELECT StatutId, Statut FROM Statut ORDER BY StatutId;

PRINT 'Etat final :';
SELECT EtatAlerteId, EtatAlerte FROM Etat ORDER BY EtatAlerteId;

-- VÃ©rifier qu'il n'y a plus de doublons
PRINT 'VÃ©rification des doublons restants :';
SELECT 'AlertType' as TableName, AlertType, COUNT(*) as Count
FROM AlertType 
GROUP BY AlertType
HAVING COUNT(*) > 1
UNION ALL
SELECT 'ExpedType' as TableName, ExpedType, COUNT(*) as Count
FROM ExpedType 
GROUP BY ExpedType
HAVING COUNT(*) > 1
UNION ALL
SELECT 'Statut' as TableName, Statut, COUNT(*) as Count
FROM Statut 
GROUP BY Statut
HAVING COUNT(*) > 1
UNION ALL
SELECT 'Etat' as TableName, EtatAlerte, COUNT(*) as Count
FROM Etat 
GROUP BY EtatAlerte
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT 'SUCCÃˆS : Tous les doublons ont Ã©tÃ© supprimÃ©s !';

PRINT 'Nettoyage terminÃ© avec succÃ¨s !';

