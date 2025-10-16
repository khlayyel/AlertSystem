-- Créer un trigger pour envoyer automatiquement les alertes après insertion
-- Ce trigger appellera l'API AlertSystem pour traiter l'envoi

-- Créer le trigger sur la table Alerte
CREATE OR ALTER TRIGGER TR_Alerte_AutoSend
ON Alerte
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AlerteId INT;
    DECLARE @TitreAlerte NVARCHAR(MAX);
    DECLARE @DescriptionAlerte NVARCHAR(MAX);
    DECLARE @AlertTypeId INT;
    DECLARE @ExpedTypeId INT;
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @ExpedTypeId = ExpedTypeId
    FROM inserted;
    
    -- Log de l'insertion
    PRINT 'TRIGGER: Nouvelle alerte détectée - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'TRIGGER: Titre: ' + @TitreAlerte;
    
    -- Insérer automatiquement dans HistoriqueAlerte pour tous les utilisateurs actifs
    INSERT INTO HistoriqueAlerte (
        AlerteId,
        DestinataireUserId,
        EtatAlerte,
        DateLecture,
        RappelSuivant,
        DestinataireEmail,
        DestinatairePhoneNumber,
        DestinataireDesktop
    )
    SELECT 
        @AlerteId,
        u.UserId,
        'Non Lu',
        NULL,
        CASE 
            WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE()) -- acquittementNécessaire = rappel dans 1h
            ELSE NULL -- acquittementNonNécessaire = pas de rappel
        END,
        u.Email,
        u.PhoneNumber,
        u.DesktopDeviceToken
    FROM Users u
    WHERE u.IsActive = 1;
    
    DECLARE @RecipientCount INT = @@ROWCOUNT;
    PRINT 'TRIGGER: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutés à l''historique';
    
    -- Appeler l'API pour envoyer les notifications
    -- Note: Ceci nécessite que l'application AlertSystem soit en cours d'exécution
    DECLARE @url NVARCHAR(500) = 'http://localhost:5000/api/v1/alerts/send-by-id/' + CAST(@AlerteId AS VARCHAR(10));
    DECLARE @response NVARCHAR(MAX);
    DECLARE @status INT;
    
    -- Essayer d'appeler l'API
    BEGIN TRY
        EXEC sp_OACreate 'MSXML2.XMLHTTP', @status OUT;
        IF @status = 0
        BEGIN
            EXEC sp_OAMethod @status, 'open', NULL, 'POST', @url, 'false';
            EXEC sp_OAMethod @status, 'setRequestHeader', NULL, 'Content-Type', 'application/json';
            EXEC sp_OAMethod @status, 'send', NULL, '{}';
            EXEC sp_OAGetProperty @status, 'responseText', @response OUT;
            EXEC sp_OADestroy @status;
            
            PRINT 'TRIGGER: API appelée avec succès - ' + @url;
            PRINT 'TRIGGER: Réponse: ' + ISNULL(@response, 'Aucune réponse');
        END
    END TRY
    BEGIN CATCH
        PRINT 'TRIGGER: Erreur lors de l''appel API - ' + ERROR_MESSAGE();
        PRINT 'TRIGGER: L''alerte a été créée mais l''envoi automatique a échoué';
        PRINT 'TRIGGER: Vous pouvez envoyer manuellement via l''interface web';
    END CATCH
    
    PRINT 'TRIGGER: Traitement terminé pour l''alerte ' + CAST(@AlerteId AS VARCHAR(10));
END;

PRINT 'Trigger TR_Alerte_AutoSend créé avec succès !';
PRINT 'Maintenant, chaque insertion dans la table Alerte déclenchera automatiquement l''envoi !';
