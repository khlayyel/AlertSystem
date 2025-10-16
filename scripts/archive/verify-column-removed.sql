-- Vérification que la colonne ExternalRecipientId a été supprimée

-- 1. Vérifier que la colonne ExternalRecipientId n'existe plus
SELECT 'Colonne ExternalRecipientId existe encore' as Check_Type,
       COUNT(*) as Count
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Destinataire' 
  AND COLUMN_NAME = 'ExternalRecipientId';

-- 2. Lister toutes les colonnes de la table Destinataire
SELECT 'Colonnes actuelles de Destinataire' as Check_Type,
       COLUMN_NAME as Column_Name,
       DATA_TYPE as Data_Type,
       IS_NULLABLE as Is_Nullable
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Destinataire'
ORDER BY ORDINAL_POSITION;

-- 3. Vérifier qu'il n'y a plus de duplications
SELECT 'Duplications dans Destinataire' as Check_Type,
       AlerteId, 
       COUNT(*) as Count
FROM Destinataire 
GROUP BY AlerteId 
HAVING COUNT(*) > 1;

-- 4. Statistiques générales
SELECT 'Statistiques finales' as Check_Type,
       (SELECT COUNT(*) FROM Alerte) as Total_Alertes,
       (SELECT COUNT(*) FROM Destinataire) as Total_Destinataires,
       (SELECT COUNT(DISTINCT AlerteId) FROM Destinataire) as Alertes_Avec_Destinataires;

-- 5. Afficher quelques exemples de données nettoyées
SELECT TOP 10 
       'Exemples de données finales' as Check_Type,
       d.DestinataireId,
       d.AlerteId,
       d.EtatAlerte,
       d.DateLecture,
       a.TitreAlerte
FROM Destinataire d
JOIN Alerte a ON d.AlerteId = a.AlerteId
ORDER BY d.DestinataireId;
