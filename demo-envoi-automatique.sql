-- DÉMONSTRATION COMPLÈTE DU SYSTÈME D'ENVOI AUTOMATIQUE
-- Ce script montre comment créer différents types d'alertes qui seront automatiquement envoyées

PRINT '🚀 === DÉMONSTRATION SYSTÈME ENVOI AUTOMATIQUE === 🚀';
PRINT '';
PRINT 'Ce script va créer 3 types d''alertes différentes pour démontrer le système :';
PRINT '1. Alerte URGENTE (avec rappel automatique)';
PRINT '2. Alerte INFORMATION (sans rappel)';
PRINT '3. Alerte MAINTENANCE (notification de service)';
PRINT '';

-- Vérifier que l'application est démarrée
PRINT '⚠️  IMPORTANT: Assurez-vous que AlertSystem est démarré sur http://localhost:5000';
PRINT '';

-- Afficher les utilisateurs qui recevront les alertes
PRINT '👥 Utilisateurs qui recevront les alertes :';
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
PRINT 'Total : ' + CAST(@UserCount AS VARCHAR(10)) + ' utilisateurs actifs';
PRINT '';

-- =====================================
-- 1. ALERTE URGENTE (Obligatoire avec rappel)
-- =====================================
PRINT '🚨 Création d''une alerte URGENTE...';

INSERT INTO Alerte (
    AlertTypeId,        -- 1 = acquittementNécessaire (avec rappel)
    ExpedTypeId,        -- 1 = Humain
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    1,                  -- Obligatoire avec rappel
    1,                  -- Humain
    '🚨 URGENCE - Intervention Requise',
    'ALERTE CRITIQUE : Un problème majeur a été détecté sur le système principal. Une intervention immédiate est requise. Merci de confirmer la réception de cette alerte et de prendre les mesures nécessaires. Temps de réponse attendu : 15 minutes maximum.',
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

DECLARE @AlerteUrgente INT = SCOPE_IDENTITY();
PRINT 'Alerte URGENTE créée avec l''ID : ' + CAST(@AlerteUrgente AS VARCHAR(10));
PRINT 'Type : Obligatoire (rappel automatique dans 1 heure)';
PRINT '';

WAITFOR DELAY '00:00:02'; -- Attendre 2 secondes

-- =====================================
-- 2. ALERTE INFORMATION (Sans rappel)
-- =====================================
PRINT '📢 Création d''une alerte INFORMATION...';

INSERT INTO Alerte (
    AlertTypeId,        -- 2 = acquittementNonNécessaire (sans rappel)
    ExpedTypeId,        -- 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    2,                  -- Information sans rappel
    2,                  -- Service
    '📢 Information - Nouvelle Fonctionnalité',
    'INFORMATION : Une nouvelle fonctionnalité d''envoi automatique d''alertes a été déployée avec succès. Vous pouvez maintenant créer des alertes directement via des requêtes SQL et elles seront automatiquement envoyées à tous les utilisateurs actifs. Cette notification est à titre informatif uniquement.',
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

DECLARE @AlerteInfo INT = SCOPE_IDENTITY();
PRINT 'Alerte INFORMATION créée avec l''ID : ' + CAST(@AlerteInfo AS VARCHAR(10));
PRINT 'Type : Information (pas de rappel)';
PRINT '';

WAITFOR DELAY '00:00:02'; -- Attendre 2 secondes

-- =====================================
-- 3. ALERTE MAINTENANCE (Notification de service)
-- =====================================
PRINT '🔧 Création d''une alerte MAINTENANCE...';

INSERT INTO Alerte (
    AlertTypeId,        -- 2 = acquittementNonNécessaire
    ExpedTypeId,        -- 2 = Service
    TitreAlerte,
    DescriptionAlerte,
    DateCreationAlerte,
    StatutId,           -- 1 = En Cours
    EtatAlerteId,       -- 2 = Non Lu
    AppId
) VALUES (
    2,                  -- Information
    2,                  -- Service
    '🔧 Maintenance Programmée - ' + FORMAT(DATEADD(DAY, 1, GETDATE()), 'dd/MM/yyyy'),
    'MAINTENANCE PROGRAMMÉE : Une maintenance du système AlertSystem est prévue demain soir de 22h00 à 02h00. Pendant cette période, le service pourra être temporairement indisponible. Aucune action n''est requise de votre part. Merci de votre compréhension.',
    GETDATE(),
    1,                  -- En Cours
    2,                  -- Non Lu
    1                   -- AppId
);

DECLARE @AlerteMaintenance INT = SCOPE_IDENTITY();
PRINT 'Alerte MAINTENANCE créée avec l''ID : ' + CAST(@AlerteMaintenance AS VARCHAR(10));
PRINT 'Type : Notification de service';
PRINT '';

-- =====================================
-- VÉRIFICATION DES RÉSULTATS
-- =====================================
PRINT '📊 Attente de la fin du traitement des triggers...';
WAITFOR DELAY '00:00:03'; -- Attendre 3 secondes

PRINT '📊 RÉSUMÉ DES ALERTES CRÉÉES :';
SELECT 
    a.AlerteId,
    a.TitreAlerte,
    at.AlertType,
    et.ExpedType,
    a.DateCreationAlerte,
    COUNT(h.DestinataireId) as NombreDestinataires
FROM Alerte a
LEFT JOIN AlertType at ON a.AlertTypeId = at.AlertTypeId
LEFT JOIN ExpedType et ON a.ExpedTypeId = et.ExpedTypeId
LEFT JOIN HistoriqueAlerte h ON a.AlerteId = h.AlerteId
WHERE a.AlerteId IN (@AlerteUrgente, @AlerteInfo, @AlerteMaintenance)
GROUP BY a.AlerteId, a.TitreAlerte, at.AlertType, et.ExpedType, a.DateCreationAlerte
ORDER BY a.AlerteId;

PRINT '';
PRINT '📧 VÉRIFICATION DES DESTINATAIRES CRÉÉS :';
SELECT 
    h.AlerteId,
    LEFT(a.TitreAlerte, 30) + '...' as Titre,
    u.FullName,
    h.DestinataireEmail,
    h.EtatAlerte,
    CASE WHEN h.RappelSuivant IS NOT NULL THEN 'Oui' ELSE 'Non' END as RappelProgramme
FROM HistoriqueAlerte h
INNER JOIN Users u ON h.DestinataireUserId = u.UserId
INNER JOIN Alerte a ON h.AlerteId = a.AlerteId
WHERE h.AlerteId IN (@AlerteUrgente, @AlerteInfo, @AlerteMaintenance)
ORDER BY h.AlerteId, u.FullName;

PRINT '';
PRINT '🎯 === DÉMONSTRATION TERMINÉE === 🎯';
PRINT '';
PRINT 'RÉSULTATS ATTENDUS :';
PRINT '✅ 3 alertes créées avec des types différents';
PRINT '✅ ' + CAST(@UserCount * 3 AS VARCHAR(10)) + ' destinataires créés au total (' + CAST(@UserCount AS VARCHAR(10)) + ' par alerte)';
PRINT '✅ Triggers exécutés automatiquement';
PRINT '✅ Tentatives d''envoi via API AlertSystem';
PRINT '';
PRINT 'VÉRIFICATIONS À EFFECTUER :';
PRINT '1. 📧 Vérifiez vos emails (Gmail, etc.)';
PRINT '2. 📱 Vérifiez vos messages WhatsApp';
PRINT '3. 🖥️  Vérifiez les notifications Web Push';
PRINT '4. 🌐 Consultez l''interface : http://localhost:5000/Home/HistoriqueTest';
PRINT '5. 📊 Vérifiez les statistiques dans l''interface web';
PRINT '';
PRINT 'Si vous recevez les 3 alertes par email/WhatsApp, le système fonctionne parfaitement ! 🎉';
PRINT '';
PRINT 'Alertes créées :';
PRINT '- Alerte URGENTE ID : ' + CAST(@AlerteUrgente AS VARCHAR(10)) + ' (avec rappel)';
PRINT '- Alerte INFO ID : ' + CAST(@AlerteInfo AS VARCHAR(10)) + ' (sans rappel)';
PRINT '- Alerte MAINTENANCE ID : ' + CAST(@AlerteMaintenance AS VARCHAR(10)) + ' (notification)';
PRINT '';
PRINT '🚀 Le système d''envoi automatique est opérationnel ! 🚀';
