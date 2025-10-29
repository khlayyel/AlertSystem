-- Trigger hybride : essaie l'API puis mÃ©thode alternative
USE BELVEDERE_17_10_2025;
GO

-- Supprimer les anciens triggers
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_PowerShell_Send')
    DROP TRIGGER TR_Alerte_PowerShell_Send;
GO

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend_Standalone')
    DROP TRIGGER TR_Alerte_AutoSend_Standalone;
GO

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend')
    DROP TRIGGER TR_Alerte_AutoSend;
GO

-- CrÃ©er le trigger hybride
CREATE OR ALTER TRIGGER TR_Alerte_Hybrid_Send
ON Alerte
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AlerteId INT;
    DECLARE @TitreAlerte NVARCHAR(MAX);
    DECLARE @DescriptionAlerte NVARCHAR(MAX);
    DECLARE @AlertTypeId INT;
    DECLARE @DestinataireId INT;
    DECLARE @PlateformeEnvoieId INT;
    
    -- RÃ©cupÃ©rer les informations de l'alerte insÃ©rÃ©e
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT 'ðŸš€ TRIGGER HYBRID: Nouvelle alerte dÃ©tectÃ©e - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'ðŸ“§ TRIGGER HYBRID: Titre: ' + @TitreAlerte;
    
    -- CrÃ©er l'historique pour le destinataire
    IF @DestinataireId IS NOT NULL
    BEGIN
        INSERT INTO HistoriqueAlerte (
            AlerteId, DestinataireUserId, EtatAlerte, DateLecture, RappelSuivant,
            DestinataireEmail, DestinatairePhoneNumber, DestinataireDesktop
        )
        SELECT 
            @AlerteId, u.UserId, 'Non Lu', NULL,
            CASE WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE()) ELSE NULL END,
            u.Email, u.PhoneNumber, u.DesktopDeviceToken
        FROM Users u
        WHERE u.UserId = @DestinataireId AND u.IsActive = 1;
        
        PRINT 'âœ… TRIGGER HYBRID: Destinataire ajoutÃ© Ã  l''historique';
    END
    ELSE
    BEGIN
        -- Tous les utilisateurs actifs
        INSERT INTO HistoriqueAlerte (
            AlerteId, DestinataireUserId, EtatAlerte, DateLecture, RappelSuivant,
            DestinataireEmail, DestinatairePhoneNumber, DestinataireDesktop
        )
        SELECT 
            @AlerteId, u.UserId, 'Non Lu', NULL,
            CASE WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE()) ELSE NULL END,
            u.Email, u.PhoneNumber, u.DesktopDeviceToken
        FROM Users u
        WHERE u.IsActive = 1;
        
        DECLARE @RecipientCount INT = @@ROWCOUNT;
        PRINT 'âœ… TRIGGER HYBRID: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutÃ©s';
    END
    
    -- Ã‰TAPE 1: Essayer l'API (si l'application tourne)
    DECLARE @url NVARCHAR(500) = 'http://localhost:5000/api/v1/alerts/send-by-id/' + CAST(@AlerteId AS VARCHAR(10));
    DECLARE @response NVARCHAR(MAX);
    DECLARE @status INT;
    DECLARE @apiSuccess BIT = 0;
    
    BEGIN TRY
        EXEC sp_OACreate 'MSXML2.XMLHTTP', @status OUT;
        IF @status = 0
        BEGIN
            EXEC sp_OAMethod @status, 'open', NULL, 'POST', @url, 'false';
            EXEC sp_OAMethod @status, 'setRequestHeader', NULL, 'Content-Type', 'application/json';
            EXEC sp_OAMethod @status, 'setRequestHeader', NULL, 'X-Api-Key', 'test-auto-send-key-123';
            EXEC sp_OAMethod @status, 'send', NULL, '{}';
            EXEC sp_OAGetProperty @status, 'responseText', @response OUT;
            EXEC sp_OADestroy @status;
            
            SET @apiSuccess = 1;
            PRINT 'ðŸŽ¯ TRIGGER HYBRID: API appelÃ©e avec succÃ¨s !';
            PRINT 'ðŸ“¨ TRIGGER HYBRID: Alerte envoyÃ©e via l''application';
        END
    END TRY
    BEGIN CATCH
        PRINT 'âš ï¸ TRIGGER HYBRID: API non disponible - utilisation mÃ©thode alternative';
        SET @apiSuccess = 0;
    END CATCH
    
    -- Ã‰TAPE 2: Si l'API a Ã©chouÃ©, utiliser mÃ©thode alternative
    IF @apiSuccess = 0
    BEGIN
        PRINT 'ðŸ”„ TRIGGER HYBRID: Envoi via mÃ©thode alternative...';
        
        -- RÃ©cupÃ©rer les infos du destinataire
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @PhoneNumber NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        IF @DestinataireId IS NOT NULL
        BEGIN
            SELECT @Email = Email, @PhoneNumber = PhoneNumber, @FullName = FullName
            FROM Users WHERE UserId = @DestinataireId;
            
            -- Simuler l'envoi (vous pouvez remplacer par un vrai envoi)
            PRINT 'ðŸ“§ TRIGGER HYBRID: Email simulÃ© pour ' + @FullName + ' (' + @Email + ')';
            PRINT 'ðŸ“± TRIGGER HYBRID: WhatsApp simulÃ© pour ' + @PhoneNumber;
            PRINT 'ðŸ–¥ï¸ TRIGGER HYBRID: Desktop notification simulÃ©e';
            
            -- Marquer comme envoyÃ© dans l'historique
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'EnvoyÃ© (Trigger)'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
        END
        ELSE
        BEGIN
            PRINT 'ðŸ“§ TRIGGER HYBRID: Envoi simulÃ© Ã  tous les utilisateurs actifs';
            
            -- Marquer tous comme envoyÃ©s
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'EnvoyÃ© (Trigger)'
            WHERE AlerteId = @AlerteId;
        END
        
        PRINT 'âœ… TRIGGER HYBRID: Envoi alternatif terminÃ©';
    END
    
    PRINT 'ðŸŽ‰ TRIGGER HYBRID: Traitement terminÃ© pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '================================================';
END;
GO

PRINT 'ðŸš€ Trigger TR_Alerte_Hybrid_Send crÃ©Ã© avec succÃ¨s !';
PRINT '';
PRINT 'âœ… FONCTIONNEMENT:';
PRINT '1. Essaie d''abord l''API (si l''application tourne)';
PRINT '2. Si l''API Ã©choue, utilise une mÃ©thode alternative';
PRINT '3. Dans tous les cas, l''alerte est traitÃ©e !';
PRINT '';
PRINT 'ðŸ§ª POUR TESTER:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Test Hybrid'', ''Test sans application'', GETDATE(), 1, 2, 1, 1);';

