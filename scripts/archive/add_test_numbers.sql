-- Ajouter des numéros de test dans PhoneNumber pour tester WhatsApp
-- Remplacez par de vrais numéros pour recevoir des messages

UPDATE Users SET PhoneNumber = '+21612345678' WHERE UserId = 1;
UPDATE Users SET PhoneNumber = '+21698765432' WHERE UserId = 2;

-- Afficher les utilisateurs
SELECT UserId, Username, Email, PhoneNumber FROM Users;
