-- CrÃ©er un trigger pour envoyer automatiquement les alertes aprÃ¨s insertion
-- Ce trigger appellera l'API AlertSystem pour traiter l'envoi

-- CrÃ©er le trigger sur la table Alerte
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
    
    -- RÃ©cupÃ©rer les informations de l'alerte insÃ©rÃ©e
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @ExpedTypeId = ExpedTypeId
    FROM inserted;
    
    -- Log de l'insertion
    PRINT 'TRIGGER: Nouvelle alerte dÃ©tectÃ©e - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'TRIGGER: Titre: ' + @TitreAlerte;
    
    -- InsÃ©rer automatiquement dans HistoriqueAlerte pour tous les utilisateurs actifs
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
            WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE()) -- acquittementNÃ©cessaire = rappel dans 1h
            ELSE NULL -- acquittementNonNÃ©cessaire = pas de rappel
        END,
        u.Email,
        u.PhoneNumber,
        u.DesktopDeviceToken
    FROM Users u
    WHERE u.IsActive = 1;
    
    DECLARE @RecipientCount INT = @@ROWCOUNT;
    PRINT 'TRIGGER: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutÃ©s Ã  l''historique';
    
    -- Appeler l'API pour envoyer les notifications
    -- Note: Ceci nÃ©cessite que l'application AlertSystem soit en cours d'exÃ©cution
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
            
            PRINT 'TRIGGER: API appelÃ©e avec succÃ¨s - ' + @url;
            PRINT 'TRIGGER: RÃ©ponse: ' + ISNULL(@response, 'Aucune rÃ©ponse');
        END
    END TRY
    BEGIN CATCH
        PRINT 'TRIGGER: Erreur lors de l''appel API - ' + ERROR_MESSAGE();
        PRINT 'TRIGGER: L''alerte a Ã©tÃ© crÃ©Ã©e mais l''envoi automatique a Ã©chouÃ©';
        PRINT 'TRIGGER: Vous pouvez envoyer manuellement via l''interface web';
    END CATCH
    
    PRINT 'TRIGGER: Traitement terminÃ© pour l''alerte ' + CAST(@AlerteId AS VARCHAR(10));
END;

PRINT 'Trigger TR_Alerte_AutoSend crÃ©Ã© avec succÃ¨s !';
PRINT 'Maintenant, chaque insertion dans la table Alerte dÃ©clenchera automatiquement l''envoi !';

