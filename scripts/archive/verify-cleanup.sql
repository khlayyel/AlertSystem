-- VÃ©rification du nettoyage des donnÃ©es

-- 1. VÃ©rifier qu'il n'y a plus de duplications dans Destinataire
SELECT 'Duplications dans Destinataire' as Check_Type,
       AlerteId, 
       COUNT(*) as Count
FROM Destinataire 
GROUP BY AlerteId 
HAVING COUNT(*) > 1;

-- 2. VÃ©rifier que tous les ExternalRecipientId sont NULL
SELECT 'ExternalRecipientId non NULL' as Check_Type,
       COUNT(*) as Count
FROM Destinataire 
WHERE ExternalRecipientId IS NOT NULL;

-- 3. Statistiques gÃ©nÃ©rales
SELECT 'Statistiques gÃ©nÃ©rales' as Check_Type,
       (SELECT COUNT(*) FROM Alerte) as Total_Alertes,
       (SELECT COUNT(*) FROM Destinataire) as Total_Destinataires,
       (SELECT COUNT(DISTINCT AlerteId) FROM Destinataire) as Alertes_Avec_Destinataires;

-- 4. VÃ©rifier la contrainte unique
SELECT 'Index unique crÃ©Ã©' as Check_Type,
       name as Index_Name
FROM sys.indexes 
WHERE object_id = OBJECT_ID('Destinataire') 
  AND name = 'IX_Destinataire_AlerteId_Unique';

-- 5. Afficher quelques exemples de donnÃ©es nettoyÃ©es
SELECT TOP 10 
       'Exemples de donnÃ©es' as Check_Type,
       d.DestinataireId,
       d.AlerteId,
       d.EtatAlerte,
       d.ExternalRecipientId,
       a.TitreAlerte
FROM Destinataire d
JOIN Alerte a ON d.AlerteId = a.AlerteId
ORDER BY d.DestinataireId;

