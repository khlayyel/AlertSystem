-- Vérification de la table Users et explication des notifications desktop

-- 1. Vérifier que la table Users a été créée
SELECT 'Structure de la table Users' as Check_Type,
       COLUMN_NAME as Column_Name,
       DATA_TYPE as Data_Type,
       IS_NULLABLE as Is_Nullable,
       CHARACTER_MAXIMUM_LENGTH as Max_Length
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

-- 2. Vérifier que Khalil a été inséré
SELECT 'Utilisateur Khalil' as Check_Type,
       UserId,
       Username,
       FullName,
       Email,
       PhoneNumber,
       WhatsAppNumber,
       DesktopDeviceToken,
       Role,
       CreatedAt,
       IsActive
FROM Users 
WHERE Username = 'khalil';

-- 3. Statistiques de la table Users
SELECT 'Statistiques Users' as Check_Type,
       COUNT(*) as Total_Users,
       COUNT(CASE WHEN IsActive = 1 THEN 1 END) as Active_Users,
       COUNT(CASE WHEN Role = 'Admin' THEN 1 END) as Admin_Users
FROM Users;

-- 4. Vérifier les index uniques
SELECT 'Index uniques' as Check_Type,
       i.name as Index_Name,
       c.name as Column_Name
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Users') 
  AND i.is_unique = 1
ORDER BY i.name, ic.key_ordinal;
