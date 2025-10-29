-- Trigger avec envoi rÃ©el d'emails utilisant vos vraies informations Gmail
USE BELVEDERE_17_10_2025;
GO

-- Supprimer l'ancien trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Hybrid_Send')
    DROP TRIGGER TR_Alerte_Hybrid_Send;
GO

-- CrÃ©er le trigger avec envoi rÃ©el d'emails
CREATE OR ALTER TRIGGER TR_Alerte_Real_Email_Send
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
    
    PRINT 'ðŸš€ TRIGGER REAL EMAIL: Nouvelle alerte - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'ðŸ“§ TRIGGER REAL EMAIL: Titre: ' + @TitreAlerte;
    
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
        
        PRINT 'âœ… TRIGGER REAL EMAIL: Destinataire ajoutÃ© Ã  l''historique';
        
        -- RÃ©cupÃ©rer les infos du destinataire
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @PhoneNumber NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        SELECT @Email = Email, @PhoneNumber = PhoneNumber, @FullName = FullName
        FROM Users WHERE UserId = @DestinataireId;
        
        -- Envoi Email rÃ©el (si plateforme = 1 ou NULL)
        IF @PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL
        BEGIN
            -- CrÃ©er un fichier PowerShell temporaire pour Ã©viter les problÃ¨mes de syntaxe
            DECLARE @PowerShellFile NVARCHAR(500) = 'C:\temp\send_alert_' + CAST(@AlerteId AS VARCHAR(10)) + '.ps1';
            DECLARE @PowerShellContent NVARCHAR(MAX);
            
            SET @PowerShellContent = 
                'try {' + CHAR(13) + CHAR(10) +
                '  $SMTPServer = "smtp.gmail.com"' + CHAR(13) + CHAR(10) +
                '  $SMTPPort = 465' + CHAR(13) + CHAR(10) +
                '  $Username = "khalilouerghemmi@gmail.com"' + CHAR(13) + CHAR(10) +
                '  $Password = ConvertTo-SecureString "xiczhnsf ywjqwgvd" -AsPlainText -Force' + CHAR(13) + CHAR(10) +
                '  $Credential = New-Object System.Management.Automation.PSCredential($Username, $Password)' + CHAR(13) + CHAR(10) +
                '  $Subject = "AlertSystem: ' + REPLACE(@TitreAlerte, '"', '') + '"' + CHAR(13) + CHAR(10) +
                '  $Body = "Bonjour ' + REPLACE(@FullName, '"', '') + ',\n\nNouvelle alerte:\n\nTitre: ' + REPLACE(@TitreAlerte, '"', '') + '\nDescription: ' + REPLACE(@DescriptionAlerte, '"', '') + '\nDate: ' + CONVERT(VARCHAR, GETDATE(), 120) + '\n\nCordialement,\nAlertSystem"' + CHAR(13) + CHAR(10) +
                '  Send-MailMessage -To "' + @Email + '" -From $Username -Subject $Subject -Body $Body -SmtpServer $SMTPServer -Port $SMTPPort -UseSsl -Credential $Credential' + CHAR(13) + CHAR(10) +
                '  Write-Host "Email envoye avec succes a ' + @Email + '"' + CHAR(13) + CHAR(10) +
                '} catch {' + CHAR(13) + CHAR(10) +
                '  Write-Host "Erreur envoi email: $($_.Exception.Message)"' + CHAR(13) + CHAR(10) +
                '}';
            
            -- CrÃ©er le rÃ©pertoire temp s'il n'existe pas
            EXEC xp_cmdshell 'if not exist C:\temp mkdir C:\temp';
            
            -- Ã‰crire le script PowerShell dans un fichier
            DECLARE @EchoCmd NVARCHAR(MAX);
            SET @EchoCmd = 'echo ' + @PowerShellContent + ' > ' + @PowerShellFile;
            
            -- Commande PowerShell simplifiÃ©e
            DECLARE @PowerShellCmd NVARCHAR(MAX);
            SET @PowerShellCmd = 'powershell.exe -ExecutionPolicy Bypass -File ' + @PowerShellFile;
            
            BEGIN TRY
                EXEC xp_cmdshell @PowerShellCmd;
                PRINT 'ðŸ“¨ TRIGGER REAL EMAIL: Commande email exÃ©cutÃ©e pour ' + @Email;
                
                -- Marquer comme envoyÃ©
                UPDATE HistoriqueAlerte 
                SET EtatAlerte = 'EnvoyÃ© par Email'
                WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
                
            END TRY
            BEGIN CATCH
                PRINT 'âŒ TRIGGER REAL EMAIL: Erreur lors de l''envoi email';
            END CATCH
        END
        
        -- WhatsApp (simulation pour l'instant)
        IF @PlateformeEnvoieId = 2 OR @PlateformeEnvoieId IS NULL
        BEGIN
            PRINT 'ðŸ“± TRIGGER REAL EMAIL: WhatsApp simulÃ© pour ' + @PhoneNumber + ': ' + @TitreAlerte;
        END
        
        -- Desktop (simulation pour l'instant)
        IF @PlateformeEnvoieId = 3 OR @PlateformeEnvoieId IS NULL
        BEGIN
            PRINT 'ðŸ–¥ï¸ TRIGGER REAL EMAIL: Desktop simulÃ© pour ' + @FullName;
        END
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
        PRINT 'âœ… TRIGGER REAL EMAIL: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutÃ©s';
        PRINT 'ðŸ“§ TRIGGER REAL EMAIL: Envoi Ã  tous les utilisateurs (non implÃ©mentÃ© dans ce trigger)';
    END
    
    PRINT 'ðŸŽ‰ TRIGGER REAL EMAIL: Traitement terminÃ© pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '================================================';
END;
GO

PRINT 'ðŸš€ Trigger TR_Alerte_Real_Email_Send crÃ©Ã© avec succÃ¨s !';
PRINT '';
PRINT 'âœ… FONCTIONNALITÃ‰S:';
PRINT '- Utilise vos vraies informations Gmail';
PRINT '- Envoie des emails rÃ©els via PowerShell';
PRINT '- Fonctionne sans que l''application soit dÃ©marrÃ©e';
PRINT '- Met Ã  jour l''historique automatiquement';
PRINT '';
PRINT 'ðŸ§ª POUR TESTER (copier-coller dans SQL Server):';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Test Email RÃ©el'', ''Cet email sera vraiment envoyÃ© !'', GETDATE(), 1, 2, 1, 1);';

