-- Vérifier la structure finale de la table Users (simplifiée)
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

-- Vérifier les données existantes (structure finale)
SELECT 
    UserId,
    FullName,
    Email,
    PhoneNumber,
    DesktopDeviceToken,
    IsActive
FROM Users
ORDER BY UserId;

-- Compter les utilisateurs actifs/inactifs
SELECT 
    IsActive,
    COUNT(*) as Count
FROM Users
GROUP BY IsActive;
