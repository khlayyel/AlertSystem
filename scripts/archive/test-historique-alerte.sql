-- Script de test pour la nouvelle structure HistoriqueAlerte
-- Vérifier que tout fonctionne correctement

-- 1. Vérifier la structure de HistoriqueAlerte
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'HistoriqueAlerte'
ORDER BY ORDINAL_POSITION;

-- 2. Vérifier les utilisateurs existants
SELECT 
    UserId,
    FullName,
    Email,
    PhoneNumber,
    IsActive
FROM Users
ORDER BY UserId;

-- 3. Vérifier les alertes existantes
SELECT 
    AlerteId,
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,
    EtatAlerteId
FROM Alerte
ORDER BY AlerteId;

-- 4. Insérer une alerte de test
INSERT INTO Alerte (
    AlertTypeId, 
    ExpedTypeId, 
    TitreAlerte, 
    DescriptionAlerte, 
    DateCreationAlerte, 
    StatutId, 
    EtatAlerteId,
    AppId
) VALUES (
    1, -- AlertTypeId (Information)
    1, -- ExpedTypeId (Service)
    'Test Alerte Multi-Destinataires',
    'Ceci est un test de la nouvelle structure HistoriqueAlerte avec plusieurs destinataires.',
    GETDATE(),
    1, -- StatutId (Envoyé)
    1, -- EtatAlerteId (Non Lu)
    1  -- AppId
);

-- Récupérer l'ID de l'alerte créée
DECLARE @AlerteId INT = SCOPE_IDENTITY();

-- 5. Insérer plusieurs destinataires pour cette alerte
INSERT INTO HistoriqueAlerte (
    AlerteId,
    DestinataireUserId,
    EtatAlerte,
    DateLecture,
    RappelSuivant,
    DestinataireEmail,
    DestinatairePhoneNumber,
    DestinataireDesktop
) VALUES 
-- Khalil (Admin) - Lu
(@AlerteId, 1, 'Lu', GETDATE(), NULL, 'khalilouerghemmi@gmail.com', '+21699414008', 'web-push-token-khalil'),
-- Zied (SuperUser) - Non Lu avec rappel
(@AlerteId, 2, 'Non Lu', NULL, DATEADD(HOUR, 1, GETDATE()), 'zied.soltani111@gmail.com', '+21621494064', 'web-push-token-zied'),
-- Sarah (User) - Non Lu
(@AlerteId, 3, 'Non Lu', NULL, NULL, 'sarah.benali@test.com', '+21620123456', 'web-push-token-sarah'),
-- Ahmed (User) - Lu
(@AlerteId, 4, 'Lu', DATEADD(MINUTE, -30, GETDATE()), NULL, 'ahmed.trabelsi@test.com', '+21625987654', 'web-push-token-ahmed'),
-- Fatma (SuperUser) - Non Lu avec rappel
(@AlerteId, 5, 'Non Lu', NULL, DATEADD(HOUR, 2, GETDATE()), 'fatma.karray@test.com', '+21622555777', 'web-push-token-fatma');

-- 6. Vérifier les données insérées
SELECT 
    h.DestinataireId,
    h.AlerteId,
    u.FullName,
    u.Email AS UserEmail,
    h.DestinataireEmail,
    h.DestinatairePhoneNumber,
    h.EtatAlerte,
    h.DateLecture,
    h.RappelSuivant,
    a.TitreAlerte
FROM HistoriqueAlerte h
INNER JOIN Users u ON h.DestinataireUserId = u.UserId
INNER JOIN Alerte a ON h.AlerteId = a.AlerteId
ORDER BY h.AlerteId, h.DestinataireId;

-- 7. Statistiques par alerte
SELECT 
    a.AlerteId,
    a.TitreAlerte,
    COUNT(h.DestinataireId) as TotalDestinataires,
    COUNT(CASE WHEN h.EtatAlerte = 'Lu' THEN 1 END) as NombreLus,
    COUNT(CASE WHEN h.EtatAlerte = 'Non Lu' THEN 1 END) as NombreNonLus,
    COUNT(CASE WHEN h.RappelSuivant IS NOT NULL THEN 1 END) as AvecRappel
FROM Alerte a
LEFT JOIN HistoriqueAlerte h ON a.AlerteId = h.AlerteId
GROUP BY a.AlerteId, a.TitreAlerte
ORDER BY a.AlerteId;

-- 8. Destinataires avec rappels en attente
SELECT 
    u.FullName,
    u.Email,
    a.TitreAlerte,
    h.RappelSuivant,
    DATEDIFF(MINUTE, GETDATE(), h.RappelSuivant) as MinutesRestantes
FROM HistoriqueAlerte h
INNER JOIN Users u ON h.DestinataireUserId = u.UserId
INNER JOIN Alerte a ON h.AlerteId = a.AlerteId
WHERE h.RappelSuivant IS NOT NULL 
  AND h.RappelSuivant > GETDATE()
  AND h.EtatAlerte = 'Non Lu'
ORDER BY h.RappelSuivant;

PRINT 'Test de la structure HistoriqueAlerte terminé avec succès !';
