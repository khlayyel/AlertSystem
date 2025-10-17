-- Mettre à jour les rôles des utilisateurs
UPDATE Users SET 
    Role = 'Admin' 
WHERE Email = 'khalilouerghemmi@gmail.com';

UPDATE Users SET 
    Role = 'SuperUser' 
WHERE Email IN ('zied.soltani111@gmail.com', 'fatma.karray@test.com');

UPDATE Users SET 
    Role = 'User' 
WHERE Email IN ('sarah.benali@test.com', 'ahmed.trabelsi@test.com');

-- Vérifier les mises à jour
SELECT UserId, Username, Email, Role, IsActive 
FROM Users 
ORDER BY 
    CASE 
        WHEN Role = 'Admin' THEN 1
        WHEN Role = 'SuperUser' THEN 2
        ELSE 3 
    END, Username;
