-- Trigger avec PowerShell pour envoi réel d'emails et notifications
USE AlertSystemDB;
GO

-- Supprimer l'ancien trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend_Standalone')
    DROP TRIGGER TR_Alerte_AutoSend_Standalone;
GO

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend')
    DROP TRIGGER TR_Alerte_AutoSend;
GO

-- Créer le nouveau trigger avec PowerShell
CREATE OR ALTER TRIGGER TR_Alerte_PowerShell_Send
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
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT 'TRIGGER POWERSHELL: Nouvelle alerte - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    
    -- Créer l'historique pour le destinataire spécifique
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
        
        -- Récupérer les infos du destinataire
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @PhoneNumber NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        SELECT @Email = Email, @PhoneNumber = PhoneNumber, @FullName = FullName
        FROM Users WHERE UserId = @DestinataireId;
        
        -- Créer le script PowerShell pour envoi
        DECLARE @PowerShellScript NVARCHAR(MAX);
        SET @PowerShellScript = 
            'try {' +
            '  $SMTPServer = "smtp.gmail.com";' +
            '  $SMTPPort = 587;' +
            '  $Username = "khalilouerghemmi@gmail.com";' +
            '  $Password = ConvertTo-SecureString "votre_mot_de_passe_app" -AsPlainText -Force;' +
            '  $Credential = New-Object System.Management.Automation.PSCredential($Username, $Password);' +
            '  $Subject = "AlertSystem: ' + REPLACE(@TitreAlerte, '"', '\"') + '";' +
            '  $Body = "Bonjour ' + REPLACE(@FullName, '"', '\"') + ',\n\nNouvelle alerte: ' + REPLACE(@TitreAlerte, '"', '\"') + '\n\nDescription: ' + REPLACE(@DescriptionAlerte, '"', '\"') + '\n\nCordialement,\nAlertSystem";' +
            '  Send-MailMessage -To "' + @Email + '" -From $Username -Subject $Subject -Body $Body -SmtpServer $SMTPServer -Port $SMTPPort -UseSsl -Credential $Credential;' +
            '  Write-Host "Email envoyé avec succès à ' + @Email + '";' +
            '} catch {' +
            '  Write-Host "Erreur envoi email: $($_.Exception.Message)";' +
            '}';
        
        -- Exécuter PowerShell pour envoi email (si plateforme = 1 ou NULL)
        IF @PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL
        BEGIN
            DECLARE @PowerShellCmd NVARCHAR(MAX);
            SET @PowerShellCmd = 'powershell.exe -Command "' + @PowerShellScript + '"';
            
            DECLARE @Result INT;
            EXEC @Result = xp_cmdshell @PowerShellCmd;
            
            PRINT 'TRIGGER POWERSHELL: Commande email exécutée pour ' + @Email;
        END
        
        -- Log pour WhatsApp (nécessiterait une API externe)
        IF @PlateformeEnvoieId = 2 OR @PlateformeEnvoieId IS NULL
        BEGIN
            PRINT 'TRIGGER POWERSHELL: WhatsApp à envoyer à ' + @PhoneNumber + ': ' + @TitreAlerte;
        END
        
        -- Log pour Desktop
        IF @PlateformeEnvoieId = 3 OR @PlateformeEnvoieId IS NULL
        BEGIN
            PRINT 'TRIGGER POWERSHELL: Notification Desktop pour ' + @FullName;
        END
    END
    
    PRINT 'TRIGGER POWERSHELL: Traitement terminé pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
END;
GO

PRINT 'Trigger TR_Alerte_PowerShell_Send créé avec succès !';
PRINT '';
PRINT 'IMPORTANT: Modifiez le mot de passe Gmail dans le script PowerShell !';
PRINT 'Remplacez "votre_mot_de_passe_app" par votre vrai mot de passe d''application Gmail.';
PRINT '';
PRINT 'Pour activer xp_cmdshell (nécessaire pour PowerShell) :';
PRINT 'EXEC sp_configure ''show advanced options'', 1; RECONFIGURE;';
PRINT 'EXEC sp_configure ''xp_cmdshell'', 1; RECONFIGURE;';
