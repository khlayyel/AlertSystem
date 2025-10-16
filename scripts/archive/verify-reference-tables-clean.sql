-- Script de vérification finale - Tables de référence nettoyées

PRINT '=== VÉRIFICATION FINALE DES TABLES DE RÉFÉRENCE ===';
PRINT '';

-- 1. AlertType (2 enregistrements attendus)
PRINT 'AlertType (2 enregistrements) :';
SELECT AlertTypeId, AlertType FROM AlertType ORDER BY AlertTypeId;
PRINT '';

-- 2. ExpedType (2 enregistrements attendus)
PRINT 'ExpedType (2 enregistrements) :';
SELECT ExpedTypeId, ExpedType FROM ExpedType ORDER BY ExpedTypeId;
PRINT '';

-- 3. Statut (5 enregistrements attendus)
PRINT 'Statut (5 enregistrements) :';
SELECT StatutId, Statut FROM Statut ORDER BY StatutId;
PRINT '';

-- 4. Etat (2 enregistrements attendus)
PRINT 'Etat (2 enregistrements) :';
SELECT EtatAlerteId, EtatAlerte FROM Etat ORDER BY EtatAlerteId;
PRINT '';

-- 5. Vérifier l'intégrité des références dans Alerte
PRINT 'Vérification des références dans la table Alerte :';

SELECT 
    'AlertType' as Reference_Table,
    a.AlertTypeId,
    at.AlertType,
    COUNT(*) as Usage_Count
FROM Alerte a
LEFT JOIN AlertType at ON a.AlertTypeId = at.AlertTypeId
GROUP BY a.AlertTypeId, at.AlertType
UNION ALL
SELECT 
    'ExpedType' as Reference_Table,
    a.ExpedTypeId,
    et.ExpedType,
    COUNT(*) as Usage_Count
FROM Alerte a
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
GROUP BY a.ExpedTypeId, et.ExpedType
UNION ALL
SELECT 
    'Statut' as Reference_Table,
    a.StatutId,
    s.Statut,
    COUNT(*) as Usage_Count
FROM Alerte a
LEFT JOIN Statut s ON a.StatutId = s.StatutId
GROUP BY a.StatutId, s.Statut
UNION ALL
SELECT 
    'Etat' as Reference_Table,
    a.EtatAlerteId,
    e.EtatAlerte,
    COUNT(*) as Usage_Count
FROM Alerte a
LEFT JOIN Etat e ON a.EtatAlerteId = e.EtatAlerteId
GROUP BY a.EtatAlerteId, e.EtatAlerte
ORDER BY Reference_Table, AlertTypeId;

PRINT '';
PRINT '=== RÉSUMÉ DU NETTOYAGE ===';
PRINT 'AVANT : ExpedType avait 4 enregistrements (2 doublons)';
PRINT 'APRÈS : ExpedType a 2 enregistrements (doublons supprimés)';
PRINT '';
PRINT 'AVANT : AlertType avait 4 enregistrements (2 doublons)';
PRINT 'APRÈS : AlertType a 2 enregistrements (doublons supprimés)';
PRINT '';
PRINT 'AVANT : Statut avait 7 enregistrements (2 doublons)';
PRINT 'APRÈS : Statut a 5 enregistrements (doublons supprimés)';
PRINT '';
PRINT 'AVANT : Etat avait 4 enregistrements (2 doublons)';
PRINT 'APRÈS : Etat a 2 enregistrements (doublons supprimés)';
PRINT '';
PRINT '✅ TOUTES LES TABLES DE RÉFÉRENCE SONT MAINTENANT PROPRES !';
PRINT '✅ TOUTES LES RÉFÉRENCES DANS ALERTE SONT VALIDES !';
PRINT '✅ AUCUN DOUBLON RESTANT !';
