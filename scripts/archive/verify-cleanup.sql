-- Vérification du nettoyage des données

-- 1. Vérifier qu'il n'y a plus de duplications dans Destinataire
SELECT 'Duplications dans Destinataire' as Check_Type,
       AlerteId, 
       COUNT(*) as Count
FROM Destinataire 
GROUP BY AlerteId 
HAVING COUNT(*) > 1;

-- 2. Vérifier que tous les ExternalRecipientId sont NULL
SELECT 'ExternalRecipientId non NULL' as Check_Type,
       COUNT(*) as Count
FROM Destinataire 
WHERE ExternalRecipientId IS NOT NULL;

-- 3. Statistiques générales
SELECT 'Statistiques générales' as Check_Type,
       (SELECT COUNT(*) FROM Alerte) as Total_Alertes,
       (SELECT COUNT(*) FROM Destinataire) as Total_Destinataires,
       (SELECT COUNT(DISTINCT AlerteId) FROM Destinataire) as Alertes_Avec_Destinataires;

-- 4. Vérifier la contrainte unique
SELECT 'Index unique créé' as Check_Type,
       name as Index_Name
FROM sys.indexes 
WHERE object_id = OBJECT_ID('Destinataire') 
  AND name = 'IX_Destinataire_AlerteId_Unique';

-- 5. Afficher quelques exemples de données nettoyées
SELECT TOP 10 
       'Exemples de données' as Check_Type,
       d.DestinataireId,
       d.AlerteId,
       d.EtatAlerte,
       d.ExternalRecipientId,
       a.TitreAlerte
FROM Destinataire d
JOIN Alerte a ON d.AlerteId = a.AlerteId
ORDER BY d.DestinataireId;
