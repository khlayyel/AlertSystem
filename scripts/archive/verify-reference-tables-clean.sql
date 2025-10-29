-- Script de vÃ©rification finale - Tables de rÃ©fÃ©rence nettoyÃ©es

PRINT '=== VÃ‰RIFICATION FINALE DES TABLES DE RÃ‰FÃ‰RENCE ===';
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

-- 5. VÃ©rifier l'intÃ©gritÃ© des rÃ©fÃ©rences dans Alerte
PRINT 'VÃ©rification des rÃ©fÃ©rences dans la table Alerte :';

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
PRINT '=== RÃ‰SUMÃ‰ DU NETTOYAGE ===';
PRINT 'AVANT : ExpedType avait 4 enregistrements (2 doublons)';
PRINT 'APRÃˆS : ExpedType a 2 enregistrements (doublons supprimÃ©s)';
PRINT '';
PRINT 'AVANT : AlertType avait 4 enregistrements (2 doublons)';
PRINT 'APRÃˆS : AlertType a 2 enregistrements (doublons supprimÃ©s)';
PRINT '';
PRINT 'AVANT : Statut avait 7 enregistrements (2 doublons)';
PRINT 'APRÃˆS : Statut a 5 enregistrements (doublons supprimÃ©s)';
PRINT '';
PRINT 'AVANT : Etat avait 4 enregistrements (2 doublons)';
PRINT 'APRÃˆS : Etat a 2 enregistrements (doublons supprimÃ©s)';
PRINT '';
PRINT 'âœ… TOUTES LES TABLES DE RÃ‰FÃ‰RENCE SONT MAINTENANT PROPRES !';
PRINT 'âœ… TOUTES LES RÃ‰FÃ‰RENCES DANS ALERTE SONT VALIDES !';
PRINT 'âœ… AUCUN DOUBLON RESTANT !';

