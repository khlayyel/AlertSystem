-- Script de test pour l'envoi automatique d'alertes via insertion SQL directe
-- Ce script va insérer une nouvelle alerte et déclencher automatiquement l'envoi

PRINT '=== TEST ENVOI AUTOMATIQUE D''ALERTE ===';
PRINT '';

-- 1. Vérifier que les utilisateurs actifs existent
PRINT 'Utilisateurs actifs disponibles :';
SELECT 
    UserId,
    FullName,
    Email,
    PhoneNumber,
    CASE WHEN DesktopDeviceToken IS NOT NULL THEN 'Oui' ELSE 'Non' END as WebPush
FROM Users 
WHERE IsActive = 1
ORDER BY UserId;

DECLARE @UserCount INT = (SELECT COUNT(*) FROM Users WHERE IsActive = 1);
PRINT 'Nombre d''utilisateurs actifs : ' + CAST(@UserCount AS VARCHAR(10));
PRINT '';

-- 2. Vérifier que l'application AlertSystem est en cours d'exécution
PRINT 'IMPORTANT: Assurez-vous que l''application AlertSystem est démarrée sur http://localhost:5000';
PRINT 'Sinon, l''envoi automatique ne fonctionnera pas !';
PRINT '';

-- 3. Insérer une alerte de test (ceci va déclencher le trigger automatiquement)
PRINT 'Insertion d''une nouvelle alerte de test...';

INSERT INTO Alerte (
    AlertTypeId,        -- 1 = acquittementNécessaire, 2 = acquittementNonNécessaire
    ExpedTypeId,        -- 1 = Humain, 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    1,                  -- acquittementNécessaire (avec rappel)
    2,                  -- Service
    'ALERTE TEST - Envoi Automatique SQL',
    'Cette alerte a été créée directement via une requête SQL INSERT. Elle devrait être envoyée automatiquement à tous les utilisateurs actifs grâce au trigger TR_Alerte_AutoSend. Test effectué le ' + CONVERT(VARCHAR(19), GETDATE(), 120),
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

-- Récupérer l'ID de l'alerte créée
DECLARE @NewAlerteId INT = SCOPE_IDENTITY();
PRINT 'Alerte créée avec l''ID : ' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

-- 4. Attendre un peu pour que le trigger se termine
WAITFOR DELAY '00:00:02'; -- Attendre 2 secondes

-- 5. Vérifier que les destinataires ont été créés automatiquement
PRINT 'Vérification des destinataires créés automatiquement :';
SELECT 
    h.DestinataireId,
    h.AlerteId,
    u.FullName,
    h.DestinataireEmail,
    h.DestinatairePhoneNumber,
    h.EtatAlerte,
    CASE WHEN h.RappelSuivant IS NOT NULL THEN 'Oui' ELSE 'Non' END as RappelProgramme
FROM HistoriqueAlerte h
INNER JOIN Users u ON h.DestinataireUserId = u.UserId
WHERE h.AlerteId = @NewAlerteId
ORDER BY h.DestinataireId;

DECLARE @RecipientCount INT = (SELECT COUNT(*) FROM HistoriqueAlerte WHERE AlerteId = @NewAlerteId);
PRINT 'Nombre de destinataires créés : ' + CAST(@RecipientCount AS VARCHAR(10));
PRINT '';

-- 6. Instructions pour vérifier l'envoi
PRINT '=== VÉRIFICATION DE L''ENVOI ===';
PRINT '1. Vérifiez vos emails (Gmail, etc.)';
PRINT '2. Vérifiez vos messages WhatsApp';
PRINT '3. Consultez les logs de l''application AlertSystem';
PRINT '4. Visitez http://localhost:5000/Home/HistoriqueTest pour voir l''interface';
PRINT '';

-- 7. Requête pour voir l'alerte dans l'interface
PRINT 'URL pour voir l''alerte dans l''interface :';
PRINT 'http://localhost:5000/Home/HistoriqueTest';
PRINT '';

-- 8. Commande pour tester manuellement l'API
PRINT 'Pour tester manuellement l''envoi via API :';
PRINT 'curl -X POST http://localhost:5000/api/v1/alerts/send-by-id/' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

PRINT '=== TEST TERMINÉ ===';
PRINT 'Alerte ID ' + CAST(@NewAlerteId AS VARCHAR(10)) + ' créée et envoi automatique déclenché !';
PRINT 'Vérifiez vos canaux de communication pour confirmer la réception.';
