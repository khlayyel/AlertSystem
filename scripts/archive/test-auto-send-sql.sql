-- Script de test pour l'envoi automatique d'alertes via insertion SQL directe
-- Ce script va insÃ©rer une nouvelle alerte et dÃ©clencher automatiquement l'envoi

PRINT '=== TEST ENVOI AUTOMATIQUE D''ALERTE ===';
PRINT '';

-- 1. VÃ©rifier que les utilisateurs actifs existent
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

-- 2. VÃ©rifier que l'application AlertSystem est en cours d'exÃ©cution
PRINT 'IMPORTANT: Assurez-vous que l''application AlertSystem est dÃ©marrÃ©e sur http://localhost:5000';
PRINT 'Sinon, l''envoi automatique ne fonctionnera pas !';
PRINT '';

-- 3. InsÃ©rer une alerte de test (ceci va dÃ©clencher le trigger automatiquement)
PRINT 'Insertion d''une nouvelle alerte de test...';

INSERT INTO Alerte (
    AlertTypeId,        -- 1 = acquittementNÃ©cessaire, 2 = acquittementNonNÃ©cessaire
    ExpedTypeId,        -- 1 = Humain, 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    1,                  -- acquittementNÃ©cessaire (avec rappel)
    2,                  -- Service
    'ALERTE TEST - Envoi Automatique SQL',
    'Cette alerte a Ã©tÃ© crÃ©Ã©e directement via une requÃªte SQL INSERT. Elle devrait Ãªtre envoyÃ©e automatiquement Ã  tous les utilisateurs actifs grÃ¢ce au trigger TR_Alerte_AutoSend. Test effectuÃ© le ' + CONVERT(VARCHAR(19), GETDATE(), 120),
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

-- RÃ©cupÃ©rer l'ID de l'alerte crÃ©Ã©e
DECLARE @NewAlerteId INT = SCOPE_IDENTITY();
PRINT 'Alerte crÃ©Ã©e avec l''ID : ' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

-- 4. Attendre un peu pour que le trigger se termine
WAITFOR DELAY '00:00:02'; -- Attendre 2 secondes

-- 5. VÃ©rifier que les destinataires ont Ã©tÃ© crÃ©Ã©s automatiquement
PRINT 'VÃ©rification des destinataires crÃ©Ã©s automatiquement :';
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
PRINT 'Nombre de destinataires crÃ©Ã©s : ' + CAST(@RecipientCount AS VARCHAR(10));
PRINT '';

-- 6. Instructions pour vÃ©rifier l'envoi
PRINT '=== VÃ‰RIFICATION DE L''ENVOI ===';
PRINT '1. VÃ©rifiez vos emails (Gmail, etc.)';
PRINT '2. VÃ©rifiez vos messages WhatsApp';
PRINT '3. Consultez les logs de l''application AlertSystem';
PRINT '4. Visitez http://localhost:5000/Home/HistoriqueTest pour voir l''interface';
PRINT '';

-- 7. RequÃªte pour voir l'alerte dans l'interface
PRINT 'URL pour voir l''alerte dans l''interface :';
PRINT 'http://localhost:5000/Home/HistoriqueTest';
PRINT '';

-- 8. Commande pour tester manuellement l'API
PRINT 'Pour tester manuellement l''envoi via API :';
PRINT 'curl -X POST http://localhost:5000/api/v1/alerts/send-by-id/' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

PRINT '=== TEST TERMINÃ‰ ===';
PRINT 'Alerte ID ' + CAST(@NewAlerteId AS VARCHAR(10)) + ' crÃ©Ã©e et envoi automatique dÃ©clenchÃ© !';
PRINT 'VÃ©rifiez vos canaux de communication pour confirmer la rÃ©ception.';

