-- TRIGGER QUI ENVOIE VRAIMENT LES EMAILS
USE AlertSystemDB;
GO

-- Supprimer l'ancien trigger
DROP TRIGGER IF EXISTS TR_Alerte_Working;
GO

-- Créer le trigger qui envoie vraiment les emails
CREATE TRIGGER TR_Alerte_Real_Sending
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
    
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT 'TRIGGER REAL: Alerte ' + CAST(@AlerteId AS VARCHAR(10)) + ' - ' + @TitreAlerte;
    
    -- Créer l'historique d'abord
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
        
        PRINT 'TRIGGER REAL: Historique créé pour utilisateur ' + CAST(@DestinataireId AS VARCHAR(10));
        
        -- ENVOYER L'EMAIL VRAIMENT (si plateforme Email)
        IF @PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL
        BEGIN
            DECLARE @Email NVARCHAR(MAX);
            DECLARE @FullName NVARCHAR(MAX);
            
            SELECT @Email = Email, @FullName = FullName
            FROM Users WHERE UserId = @DestinataireId;
            
            -- Créer le fichier PowerShell temporaire
            DECLARE @PSFile NVARCHAR(500) = 'C:\temp\send_email_' + CAST(@AlerteId AS VARCHAR(10)) + '.ps1';
            DECLARE @PSContent NVARCHAR(MAX);
            
            SET @PSContent = 
                'try {' + CHAR(13) + CHAR(10) +
                '  $smtp = "smtp.gmail.com"' + CHAR(13) + CHAR(10) +
                '  $port = 587' + CHAR(13) + CHAR(10) +
                '  $user = "khalilouerghemmi@gmail.com"' + CHAR(13) + CHAR(10) +
                '  $pass = ConvertTo-SecureString "xiczhnsf ywjqwgvd" -AsPlainText -Force' + CHAR(13) + CHAR(10) +
                '  $cred = New-Object System.Management.Automation.PSCredential($user, $pass)' + CHAR(13) + CHAR(10) +
                '  $subject = "AlertSystem: ' + REPLACE(@TitreAlerte, '"', '') + '"' + CHAR(13) + CHAR(10) +
                '  $body = "Bonjour ' + REPLACE(@FullName, '"', '') + ',\n\nNouvelle alerte:\n\n' + REPLACE(@TitreAlerte, '"', '') + '\n\n' + REPLACE(@DescriptionAlerte, '"', '') + '\n\nCordialement,\nAlertSystem"' + CHAR(13) + CHAR(10) +
                '  Send-MailMessage -To "' + @Email + '" -From $user -Subject $subject -Body $body -SmtpServer $smtp -Port $port -UseSsl -Credential $cred' + CHAR(13) + CHAR(10) +
                '  Write-Host "EMAIL ENVOYE AVEC SUCCES A ' + @Email + '"' + CHAR(13) + CHAR(10) +
                '} catch {' + CHAR(13) + CHAR(10) +
                '  Write-Host "ERREUR EMAIL: $($_.Exception.Message)"' + CHAR(13) + CHAR(10) +
                '}';
            
            -- Créer le répertoire temp
            EXEC xp_cmdshell 'if not exist C:\temp mkdir C:\temp', NO_OUTPUT;
            
            -- Écrire le fichier PowerShell
            DECLARE @WriteCmd NVARCHAR(MAX);
            SET @WriteCmd = 'echo ' + REPLACE(@PSContent, '"', '""') + ' > ' + @PSFile;
            EXEC xp_cmdshell @WriteCmd, NO_OUTPUT;
            
            -- Exécuter PowerShell
            DECLARE @ExecCmd NVARCHAR(MAX);
            SET @ExecCmd = 'powershell.exe -ExecutionPolicy Bypass -File ' + @PSFile;
            
            BEGIN TRY
                EXEC xp_cmdshell @ExecCmd;
                PRINT 'TRIGGER REAL: Email envoyé à ' + @Email;
                
                -- Marquer comme envoyé
                UPDATE HistoriqueAlerte 
                SET EtatAlerte = 'Envoyé par Email'
                WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
                
                -- Nettoyer le fichier temporaire
                EXEC xp_cmdshell ('del ' + @PSFile), NO_OUTPUT;
                
            END TRY
            BEGIN CATCH
                PRINT 'TRIGGER REAL: Erreur envoi email';
                
                UPDATE HistoriqueAlerte 
                SET EtatAlerte = 'Erreur envoi'
                WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
            END CATCH
        END
        ELSE
        BEGIN
            PRINT 'TRIGGER REAL: Pas d''envoi email (plateforme=' + CAST(@PlateformeEnvoieId AS VARCHAR(10)) + ')';
        END
    END
    
    PRINT 'TRIGGER REAL: Traitement terminé';
END;
GO

PRINT 'Trigger TR_Alerte_Real_Sending créé !';
PRINT 'Ce trigger envoie VRAIMENT les emails !';
PRINT '';
PRINT 'Test maintenant:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''TEST EMAIL REEL'', ''Cet email sera vraiment envoyé!'', GETDATE(), 1, 2, 1, 1);';
