-- TRIGGER SIMPLE QUI ENVOIE VRAIMENT LES EMAILS
USE AlertSystemDB;
GO

DROP TRIGGER IF EXISTS TR_Alerte_Working;
DROP TRIGGER IF EXISTS TR_Alerte_Real_Sending;
GO

CREATE TRIGGER TR_Alerte_Email_Sender
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
    
    PRINT 'EMAIL TRIGGER: Alerte ' + CAST(@AlerteId AS VARCHAR(10));
    
    -- Créer l'historique
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
        
        -- Envoyer email si plateforme = Email
        IF @PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL
        BEGIN
            DECLARE @Email NVARCHAR(MAX);
            DECLARE @FullName NVARCHAR(MAX);
            
            SELECT @Email = Email, @FullName = FullName
            FROM Users WHERE UserId = @DestinataireId;
            
            -- Commande PowerShell simple
            DECLARE @PSCmd NVARCHAR(MAX);
            SET @PSCmd = 'powershell.exe -Command "Send-MailMessage -To ''' + @Email + ''' -From ''khalilouerghemmi@gmail.com'' -Subject ''AlertSystem: ' + @TitreAlerte + ''' -Body ''Bonjour ' + @FullName + ', Nouvelle alerte: ' + @TitreAlerte + ''' -SmtpServer ''smtp.gmail.com'' -Port 587 -UseSsl -Credential (New-Object PSCredential(''khalilouerghemmi@gmail.com'', (ConvertTo-SecureString ''xiczhnsf ywjqwgvd'' -AsPlainText -Force)))"';
            
            BEGIN TRY
                EXEC xp_cmdshell @PSCmd;
                PRINT 'EMAIL TRIGGER: Email envoyé à ' + @Email;
                
                UPDATE HistoriqueAlerte 
                SET EtatAlerte = 'Envoyé par Email'
                WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
                
            END TRY
            BEGIN CATCH
                PRINT 'EMAIL TRIGGER: Erreur envoi';
            END CATCH
        END
    END
    
    PRINT 'EMAIL TRIGGER: Terminé';
END;
GO

PRINT 'Trigger email créé !';
PRINT 'Testez maintenant:';
