-- Vérifier la nouvelle structure HistoriqueAlerte après restructuration
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'HistoriqueAlerte'
ORDER BY ORDINAL_POSITION;

-- Vérifier les données dans HistoriqueAlerte
SELECT 
    DestinataireId,
    AlerteId,
    DestinataireUserId,
    EtatAlerte,
    DateLecture,
    RappelSuivant,
    DestinataireEmail,
    DestinatairePhoneNumber,
    DestinataireDesktop
FROM HistoriqueAlerte
ORDER BY AlerteId, DestinataireId;

-- Vérifier que les colonnes ont été supprimées de la table Alerte
SELECT 
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Alerte'
  AND COLUMN_NAME IN ('DestinataireEmail', 'DestinatairePhoneNumber', 'DestinataireDesktop', 'DateLecture', 'RappelSuivant')
ORDER BY COLUMN_NAME;

-- Compter les enregistrements par alerte dans HistoriqueAlerte
SELECT 
    AlerteId,
    COUNT(*) as NombreDestinataires,
    COUNT(CASE WHEN DateLecture IS NOT NULL THEN 1 END) as NombreLus,
    COUNT(CASE WHEN DateLecture IS NULL THEN 1 END) as NombreNonLus
FROM HistoriqueAlerte
GROUP BY AlerteId
ORDER BY AlerteId;

-- Vérifier les relations avec Users
SELECT 
    h.DestinataireId,
    h.AlerteId,
    h.DestinataireUserId,
    u.FullName,
    u.Email,
    h.DestinataireEmail,
    h.EtatAlerte
FROM HistoriqueAlerte h
LEFT JOIN Users u ON h.DestinataireUserId = u.UserId
ORDER BY h.AlerteId, h.DestinataireId;
