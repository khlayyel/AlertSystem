-- Script pour vérifier tous les doublons dans les tables de référence

-- 1. Vérifier AlertType
SELECT 'AlertType' as TableName, AlertType, COUNT(*) as Count
FROM AlertType 
GROUP BY AlertType
HAVING COUNT(*) > 1
UNION ALL

-- 2. Vérifier ExpedType (déjà nettoyé)
SELECT 'ExpedType' as TableName, ExpedType, COUNT(*) as Count
FROM ExpedType 
GROUP BY ExpedType
HAVING COUNT(*) > 1
UNION ALL

-- 3. Vérifier Statut
SELECT 'Statut' as TableName, Statut, COUNT(*) as Count
FROM Statut 
GROUP BY Statut
HAVING COUNT(*) > 1
UNION ALL

-- 4. Vérifier Etat
SELECT 'Etat' as TableName, EtatAlerte, COUNT(*) as Count
FROM Etat 
GROUP BY EtatAlerte
HAVING COUNT(*) > 1;

-- Si aucun résultat, alors pas de doublons
IF @@ROWCOUNT = 0
    PRINT 'Aucun doublon trouvé dans les tables de référence !';

-- Afficher le contenu de toutes les tables de référence
PRINT 'Contenu des tables de référence :';

PRINT 'AlertType :';
SELECT AlertTypeId, AlertType FROM AlertType ORDER BY AlertTypeId;

PRINT 'ExpedType :';
SELECT ExpedTypeId, ExpedType FROM ExpedType ORDER BY ExpedTypeId;

PRINT 'Statut :';
SELECT StatutId, Statut FROM Statut ORDER BY StatutId;

PRINT 'Etat :';
SELECT EtatAlerteId, EtatAlerte FROM Etat ORDER BY EtatAlerteId;
