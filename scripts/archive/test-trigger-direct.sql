-- Test direct du trigger d'envoi automatique via insertion SQL

PRINT '=== TEST TRIGGER ENVOI AUTOMATIQUE ===';
PRINT 'Ce test va insérer une nouvelle alerte et déclencher automatiquement l''envoi';
PRINT '';

-- 1. Vérifier les utilisateurs actifs
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

-- 2. Insérer une nouvelle alerte (déclenche automatiquement le trigger)
PRINT 'Insertion d''une nouvelle alerte de test...';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(19), GETDATE(), 120);

INSERT INTO Alerte (
    AlertTypeId,        -- 2 = acquittementNonNécessaire (pas de rappel)
    ExpedTypeId,        -- 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    2,                  -- acquittementNonNécessaire
    2,                  -- Service
    'TEST TRIGGER - Envoi Automatique Direct',
    'Cette alerte teste le trigger SQL qui doit automatiquement créer les destinataires et déclencher l''envoi via l''API AlertSystem. Test effectué le ' + CONVERT(VARCHAR(19), GETDATE(), 120) + '. Si vous recevez cette alerte par email ou WhatsApp, le système fonctionne parfaitement !',
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

-- Récupérer l'ID de l'alerte créée
DECLARE @NewAlerteId INT = SCOPE_IDENTITY();
PRINT 'Alerte créée avec l''ID : ' + CAST(@NewAlerteId AS VARCHAR(10));
PRINT '';

-- 3. Attendre que le trigger se termine
PRINT 'Attente de la fin du traitement du trigger...';
WAITFOR DELAY '00:00:03'; -- Attendre 3 secondes

-- 4. Vérifier que les destinataires ont été créés
PRINT 'Vérification des destinataires créés par le trigger :';
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
PRINT 'Nombre de destinataires créés par le trigger : ' + CAST(@RecipientCount AS VARCHAR(10));
PRINT '';

-- 5. Afficher les informations de l'alerte créée
PRINT 'Détails de l''alerte créée :';
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
PRINT '=== RÉSULTATS ATTENDUS ===';
PRINT '1. Le trigger a automatiquement créé ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires';
PRINT '2. Le trigger a tenté d''appeler l''API pour envoyer les notifications';
PRINT '3. Vérifiez vos emails et messages WhatsApp pour confirmer la réception';
PRINT '4. Consultez l''interface web : http://localhost:5000/Home/HistoriqueTest';
PRINT '';
PRINT 'Si vous ne recevez pas les notifications :';
PRINT '- Vérifiez que l''application AlertSystem est démarrée';
PRINT '- Vérifiez les logs de l''application pour les erreurs';
PRINT '- Le trigger a créé les destinataires mais l''envoi API peut avoir échoué';
PRINT '';
PRINT 'TEST TERMINÉ - Alerte ID ' + CAST(@NewAlerteId AS VARCHAR(10)) + ' créée avec succès !';
