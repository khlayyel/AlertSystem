-- Test direct du trigger d'envoi automatique via insertion SQL

PRINT '=== TEST TRIGGER ENVOI AUTOMATIQUE ===';
PRINT 'Ce test va insÃ©rer une nouvelle alerte et dÃ©clencher automatiquement l''envoi';
PRINT '';

-- 1. VÃ©rifier les utilisateurs actifs
PRINT 'Utilisateurs actifs qui recevront l''alerte :';
SELECT 
    UserId,
    FullName,
    Email,
    PhoneNumber
FROM Users 
WHERE IsActive = 1
ORDER BY UserId;

DECLARE @UserCount INT = (SELECT COUNT(*) FROM Users WHERE IsActive = 1);
PRINT 'Nombre d''utilisateurs actifs : ' + CAST(@UserCount AS VARCHAR(10));
PRINT '';

-- 2. InsÃ©rer une nouvelle alerte (dÃ©clenche automatiquement le trigger)
PRINT 'Insertion d''une nouvelle alerte de test...';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(19), GETDATE(), 120);

INSERT INTO Alerte (
    AlertTypeId,        -- 2 = acquittementNonNÃ©cessaire (pas de rappel)
    ExpedTypeId,        -- 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    2,                  -- acquittementNonNÃ©cessaire
    2,                  -- Service
    'TEST TRIGGER - Envoi Automatique Direct',
    'Cette alerte teste le trigger SQL qui doit automatiquement crÃ©er les destinataires et dÃ©clencher l''envoi via l''API AlertSystem. Test effectuÃ© le ' + CONVERT(VARCHAR(19), GETDATE(), 120) + '. Si vous recevez cette alerte par email ou WhatsApp, le systÃ¨me fonctionne parfaitement !',
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

-- RÃ©cupÃ©rer l'ID de l'alerte crÃ©Ã©e
DECLARE @NewAlerteId INT = SCOPE_IDENTITY();
PRINT 'Alerte crÃ©Ã©e avec l''ID : ' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

-- 3. Attendre que le trigger se termine
PRINT 'Attente de la fin du traitement du trigger...';
WAITFOR DELAY '00:00:03'; -- Attendre 3 secondes

-- 4. VÃ©rifier que les destinataires ont Ã©tÃ© crÃ©Ã©s
PRINT 'VÃ©rification des destinataires crÃ©Ã©s par le trigger :';
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
PRINT 'Nombre de destinataires crÃ©Ã©s par le trigger : ' + CAST(@RecipientCount AS VARCHAR(10));
PRINT '';

-- 5. Afficher les informations de l'alerte crÃ©Ã©e
PRINT 'DÃ©tails de l''alerte crÃ©Ã©e :';
SELECT 
    a.AlerteId,
    a.TitreAlerte,
    a.DescriptionAlerte,
    a.DateCreationAlerte,
    at.AlertType,
    et.ExpedType,
    s.Statut,
    e.EtatAlerte
FROM Alerte a
LEFT JOIN AlertType at ON a.AlertTypeId = at.AlertTypeId
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
LEFT JOIN Statut s ON a.StatutId = s.StatutId
LEFT JOIN Etat e ON a.EtatAlerteId = e.EtatAlerteId
WHERE a.AlerteId = @NewAlerteId;

PRINT '';
PRINT '=== RÃ‰SULTATS ATTENDUS ===';
PRINT '1. Le trigger a automatiquement crÃ©Ã© ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires';
PRINT '2. Le trigger a tentÃ© d''appeler l''API pour envoyer les notifications';
PRINT '3. VÃ©rifiez vos emails et messages WhatsApp pour confirmer la rÃ©ception';
PRINT '4. Consultez l''interface web : http://localhost:5000/Home/HistoriqueTest';
PRINT '';
PRINT 'Si vous ne recevez pas les notifications :';
PRINT '- VÃ©rifiez que l''application AlertSystem est dÃ©marrÃ©e';
PRINT '- VÃ©rifiez les logs de l''application pour les erreurs';
PRINT '- Le trigger a crÃ©Ã© les destinataires mais l''envoi API peut avoir Ã©chouÃ©';
PRINT '';
PRINT 'TEST TERMINÃ‰ - Alerte ID ' + CAST(@NewAlerteId AS VARCHAR(10)) + ' crÃ©Ã©e avec succÃ¨s !';

