-- Vérification de tous les utilisateurs ajoutés

-- 1. Lister tous les utilisateurs
SELECT 'Tous les utilisateurs' as Check_Type,
       UserId,
       Username,
       FullName,
       Email,
       PhoneNumber,
       WhatsAppNumber,
       LEFT(DesktopDeviceToken, 30) + '...' as DeviceToken_Preview,
       Role,
       IsActive,
       CreatedAt
FROM Users
ORDER BY UserId;

-- 2. Statistiques par rôle
SELECT 'Statistiques par rôle' as Check_Type,
       Role,
       COUNT(*) as Count
FROM Users
WHERE IsActive = 1
GROUP BY Role;

-- 3. Vérifier les emails uniques
SELECT 'Emails en doublon' as Check_Type,
       Email,
       COUNT(*) as Count
FROM Users
GROUP BY Email
HAVING COUNT(*) > 1;

-- 4. Vérifier les usernames uniques
SELECT 'Usernames en doublon' as Check_Type,
       Username,
       COUNT(*) as Count
FROM Users
GROUP BY Username
HAVING COUNT(*) > 1;

-- 5. Utilisateurs avec numéros WhatsApp
SELECT 'Utilisateurs WhatsApp' as Check_Type,
       Username,
       FullName,
       WhatsAppNumber
FROM Users
WHERE WhatsAppNumber IS NOT NULL
ORDER BY Username;

-- 6. Résumé final
SELECT 'Résumé final' as Check_Type,
       COUNT(*) as Total_Users,
       COUNT(CASE WHEN Role = 'Admin' THEN 1 END) as Admins,
       COUNT(CASE WHEN Role = 'SuperUser' THEN 1 END) as SuperUsers,
       COUNT(CASE WHEN Role = 'User' THEN 1 END) as Users,
       COUNT(CASE WHEN IsActive = 1 THEN 1 END) as Active_Users,
       COUNT(CASE WHEN WhatsAppNumber IS NOT NULL THEN 1 END) as Users_With_WhatsApp
FROM Users;
