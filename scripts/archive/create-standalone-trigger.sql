-- Trigger SQL autonome pour envoi direct sans application
-- Ce trigger envoie directement les notifications via SQL Server

USE AlertSystemDB;
GO

-- Supprimer l'ancien trigger s'il existe
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend')
    DROP TRIGGER TR_Alerte_AutoSend;
GO

-- Créer le nouveau trigger autonome
CREATE OR ALTER TRIGGER TR_Alerte_AutoSend_Standalone
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
    DECLARE @DestinataireId INT;
    DECLARE @PlateformeEnvoieId INT;
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @ExpedTypeId = ExpedTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT 'TRIGGER STANDALONE: Nouvelle alerte détectée - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'TRIGGER STANDALONE: Titre: ' + @TitreAlerte;
    
    -- Si DestinataireId est spécifié, créer pour ce destinataire uniquement
    IF @DestinataireId IS NOT NULL
    BEGIN
        PRINT 'TRIGGER STANDALONE: Destinataire spécifique - UserId: ' + CAST(@DestinataireId AS VARCHAR(10));
        
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
                WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE())
                ELSE NULL
            END,
            u.Email,
            u.PhoneNumber,
            u.DesktopDeviceToken
        FROM Users u
        WHERE u.UserId = @DestinataireId AND u.IsActive = 1;
        
        DECLARE @RecipientCount INT = @@ROWCOUNT;
        PRINT 'TRIGGER STANDALONE: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataire ajouté';
        
        -- Envoi direct selon la plateforme
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @PhoneNumber NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        SELECT @Email = Email, @PhoneNumber = PhoneNumber, @FullName = FullName
        FROM Users WHERE UserId = @DestinataireId;
        
        -- Envoi Email direct via SQL Server (si PlateformeEnvoieId = 1 ou NULL)
        IF @PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL
        BEGIN
            DECLARE @EmailSubject NVARCHAR(255) = 'AlertSystem: ' + @TitreAlerte;
            DECLARE @EmailBody NVARCHAR(MAX) = 
                'Bonjour ' + @FullName + ',' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
                'Nouvelle alerte: ' + @TitreAlerte + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
                'Description: ' + @DescriptionAlerte + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
                'Date: ' + CONVERT(VARCHAR, GETDATE(), 120) + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
                'Cordialement,' + CHAR(13) + CHAR(10) +
                'AlertSystem';
            
            BEGIN TRY
                EXEC msdb.dbo.sp_send_dbmail
                    @profile_name = 'AlertSystem',
                    @recipients = @Email,
                    @subject = @EmailSubject,
                    @body = @EmailBody;
                
                PRINT 'TRIGGER STANDALONE: Email envoyé à ' + @Email;
            END TRY
            BEGIN CATCH
                PRINT 'TRIGGER STANDALONE: Erreur envoi email - ' + ERROR_MESSAGE();
            END CATCH
        END
        
        -- Envoi WhatsApp via API externe (si PlateformeEnvoieId = 2 ou NULL)
        IF @PlateformeEnvoieId = 2 OR @PlateformeEnvoieId IS NULL
        BEGIN
            DECLARE @WhatsAppMessage NVARCHAR(MAX) = 
                '🚨 *' + @TitreAlerte + '*' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
                @DescriptionAlerte;
            
            -- Ici vous pouvez ajouter l'appel à l'API WhatsApp
            PRINT 'TRIGGER STANDALONE: WhatsApp à envoyer à ' + @PhoneNumber + ': ' + @WhatsAppMessage;
        END
        
        -- Notification Desktop (si PlateformeEnvoieId = 3 ou NULL)
        IF @PlateformeEnvoieId = 3 OR @PlateformeEnvoieId IS NULL
        BEGIN
            PRINT 'TRIGGER STANDALONE: Notification Desktop pour ' + @FullName;
        END
    END
    ELSE
    BEGIN
        -- Comportement par défaut : tous les utilisateurs actifs
        PRINT 'TRIGGER STANDALONE: Envoi à tous les utilisateurs actifs';
        
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
                WHEN @AlertTypeId = 1 THEN DATEADD(HOUR, 1, GETDATE())
                ELSE NULL
            END,
            u.Email,
            u.PhoneNumber,
            u.DesktopDeviceToken
        FROM Users u
        WHERE u.IsActive = 1;
        
        SET @RecipientCount = @@ROWCOUNT;
        PRINT 'TRIGGER STANDALONE: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutés';
    END
    
    PRINT 'TRIGGER STANDALONE: Traitement terminé pour l''alerte ' + CAST(@AlerteId AS VARCHAR(10));
END;
GO

PRINT 'Trigger TR_Alerte_AutoSend_Standalone créé avec succès !';
PRINT 'Ce trigger fonctionne sans avoir besoin de démarrer l''application !';
PRINT '';
PRINT 'Pour configurer l''envoi d''emails, exécutez aussi le script de configuration Database Mail.';
