-- Script de test pour les nouvelles colonnes PlateformeEnvoieId et DestinataireId
-- Utilisation : sqlcmd -S "(localdb)\MSSQLLocalDB" -d "AlertSystemDB" -i "test-nouvelles-colonnes.sql"

USE AlertSystemDB;
GO

PRINT '=== TEST DES NOUVELLES COLONNES ===';
PRINT '';

-- Afficher les plateformes disponibles
PRINT '1. PLATEFORMES D''ENVOI DISPONIBLES :';
SELECT PlateformeId, Plateforme FROM PlateformeEnvoie ORDER BY PlateformeId;
PRINT '';

-- Afficher les utilisateurs disponibles
PRINT '2. UTILISATEURS DISPONIBLES :';
SELECT UserId, FullName, Email FROM Users WHERE IsActive = 1 ORDER BY UserId;
PRINT '';

-- Test 1: Alerte Email pour Zied (UserId=2)
PRINT '3. TEST 1 - Alerte Email pour Zied Soltani :';
INSERT INTO Alerte (
    AlertTypeId,        -- 1 = Obligatoire
    ExpedTypeId,        -- 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId,
    PlateformeEnvoieId, -- 1 = Email
    DestinataireId      -- 2 = Zied Soltani
) VALUES (
    1,
    2,
    'TEST - Alerte Email pour Zied',
    'Cette alerte sera envoyée uniquement par Email à Zied Soltani',
    GETDATE(),
    1,
    2,
    1,
    1,  -- Email
    2   -- Zied
);

DECLARE @AlerteId1 INT = SCOPE_IDENTITY();
PRINT 'Alerte créée avec ID: ' + CAST(@AlerteId1 AS VARCHAR(10));
PRINT '';

-- Test 2: Alerte WhatsApp pour Sarah (UserId=3)
PRINT '4. TEST 2 - Alerte WhatsApp pour Sarah Ben Ali :';
INSERT INTO Alerte (
    AlertTypeId,
    ExpedTypeId,
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,
    EtatAlerteId,
    AppId,
    PlateformeEnvoieId, -- 2 = WhatsApp
    DestinataireId      -- 3 = Sarah Ben Ali
) VALUES (
    2,  -- Information
    1,  -- Humain
    'TEST - Alerte WhatsApp pour Sarah',
    'Cette alerte sera envoyée uniquement par WhatsApp à Sarah Ben Ali',
    GETDATE(),
    1,
    2,
    1,
    2,  -- WhatsApp
    3   -- Sarah
);

DECLARE @AlerteId2 INT = SCOPE_IDENTITY();
PRINT 'Alerte créée avec ID: ' + CAST(@AlerteId2 AS VARCHAR(10));
PRINT '';

-- Test 3: Alerte Desktop pour Khalil (UserId=1)
PRINT '5. TEST 3 - Alerte Desktop pour Khalil Ouerghemmi :';
INSERT INTO Alerte (
    AlertTypeId,
    ExpedTypeId,
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,
    EtatAlerteId,
    AppId,
    PlateformeEnvoieId, -- 3 = Desktop
    DestinataireId      -- 1 = Khalil
) VALUES (
    1,  -- Obligatoire
    2,  -- Service
    'TEST - Alerte Desktop pour Khalil',
    'Cette alerte sera envoyée uniquement par notification Desktop à Khalil',
    GETDATE(),
    1,
    2,
    1,
    3,  -- Desktop
    1   -- Khalil
);

DECLARE @AlerteId3 INT = SCOPE_IDENTITY();
PRINT 'Alerte créée avec ID: ' + CAST(@AlerteId3 AS VARCHAR(10));
PRINT '';

-- Afficher les résultats avec jointures
PRINT '6. RÉSULTATS - ALERTES AVEC PLATEFORMES ET DESTINATAIRES :';
SELECT 
    a.AlerteId,
    a.TitreAlerte,
    pe.Plateforme AS 'Plateforme d''envoi',
    u.FullName AS 'Destinataire',
    u.Email AS 'Email destinataire',
    at.AlertTypeName AS 'Type alerte',
    a.DateCreationAlerte
FROM Alerte a
LEFT JOIN PlateformeEnvoie pe ON a.PlateformeEnvoieId = pe.PlateformeId
LEFT JOIN Users u ON a.DestinataireId = u.UserId
LEFT JOIN AlertType at ON a.AlertTypeId = at.AlertTypeId
WHERE a.AlerteId IN (@AlerteId1, @AlerteId2, @AlerteId3)
ORDER BY a.AlerteId;

PRINT '';
PRINT '=== TESTS TERMINÉS AVEC SUCCÈS ! ===';
PRINT '';
PRINT 'Maintenant vous pouvez :';
PRINT '1. Spécifier la plateforme d''envoi (Email=1, WhatsApp=2, Desktop=3)';
PRINT '2. Spécifier le destinataire spécifique (UserId)';
PRINT '3. Combiner les deux pour un envoi ciblé';
