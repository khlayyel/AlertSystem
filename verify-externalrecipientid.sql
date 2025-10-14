-- Vérification que ExternalRecipientId = DestinataireId

-- 1. Vérifier que tous les ExternalRecipientId correspondent aux DestinataireId
SELECT 'ExternalRecipientId != DestinataireId' as Check_Type,
       COUNT(*) as Count
FROM Destinataire 
WHERE ExternalRecipientId != CAST(DestinataireId AS NVARCHAR(50));

-- 2. Vérifier qu'il n'y a pas de valeurs NULL
SELECT 'ExternalRecipientId NULL' as Check_Type,
       COUNT(*) as Count
FROM Destinataire 
WHERE ExternalRecipientId IS NULL;

-- 3. Afficher quelques exemples
SELECT TOP 10 
       'Exemples de correspondance' as Check_Type,
       DestinataireId,
       ExternalRecipientId,
       CASE 
           WHEN ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50)) 
           THEN 'CORRECT' 
           ELSE 'INCORRECT' 
       END as Status,
       AlerteId,
       EtatAlerte
FROM Destinataire
ORDER BY DestinataireId;

-- 4. Statistiques générales
SELECT 'Statistiques' as Check_Type,
       COUNT(*) as Total_Destinataires,
       COUNT(CASE WHEN ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50)) THEN 1 END) as Correct_Mappings,
       COUNT(CASE WHEN ExternalRecipientId != CAST(DestinataireId AS NVARCHAR(50)) THEN 1 END) as Incorrect_Mappings
FROM Destinataire;
