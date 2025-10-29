-- Script pour vÃ©rifier tous les doublons dans les tables de rÃ©fÃ©rence

-- 1. VÃ©rifier AlertType
SELECT 'AlertType' as TableName, AlertType, COUNT(*) as Count
FROM AlertType 
GROUP BY AlertType
HAVING COUNT(*) > 1
UNION ALL

-- 2. VÃ©rifier ExpedType (dÃ©jÃ  nettoyÃ©)
SELECT 'ExpedType' as TableName, ExpedType, COUNT(*) as Count
FROM ExpedType 
GROUP BY ExpedType
HAVING COUNT(*) > 1
UNION ALL

-- 3. VÃ©rifier Statut
SELECT 'Statut' as TableName, Statut, COUNT(*) as Count
FROM Statut 
GROUP BY Statut
HAVING COUNT(*) > 1
UNION ALL

-- 4. VÃ©rifier Etat
SELECT 'Etat' as TableName, EtatAlerte, COUNT(*) as Count
FROM Etat 
GROUP BY EtatAlerte
HAVING COUNT(*) > 1;

-- Si aucun rÃ©sultat, alors pas de doublons
IF @@ROWCOUNT = 0
    PRINT 'Aucun doublon trouvÃ© dans les tables de rÃ©fÃ©rence !';

-- Afficher le contenu de toutes les tables de rÃ©fÃ©rence
PRINT 'Contenu des tables de rÃ©fÃ©rence :';

PRINT 'AlertType :';
SELECT AlertTypeId, AlertType FROM AlertType ORDER BY AlertTypeId;

PRINT 'ExpedType :';
SELECT ExpedTypeId, ExpedType FROM ExpedType ORDER BY ExpedTypeId;

PRINT 'Statut :';
SELECT StatutId, Statut FROM Statut ORDER BY StatutId;

PRINT 'Etat :';
SELECT EtatAlerteId, EtatAlerte FROM Etat ORDER BY EtatAlerteId;

