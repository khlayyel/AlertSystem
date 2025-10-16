-- Trigger simple avec envoi d'email réel
USE AlertSystemDB;
GO

-- Supprimer l'ancien trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Real_Email_Send')
    DROP TRIGGER TR_Alerte_Real_Email_Send;
GO

-- Créer le trigger simple
CREATE OR ALTER TRIGGER TR_Alerte_Simple_Email
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
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId
    FROM inserted;
    
    PRINT 'TRIGGER: Nouvelle alerte - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    
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
        DECLARE @FullName NVARCHAR(MAX);
        
        SELECT @Email = Email, @FullName = FullName
        FROM Users WHERE UserId = @DestinataireId;
        
        -- Créer un script PowerShell simple
        DECLARE @PSScript NVARCHAR(MAX);
        SET @PSScript = 'Send-MailMessage -To "' + @Email + '" -From "khalilouerghemmi@gmail.com" -Subject "AlertSystem: ' + @TitreAlerte + '" -Body "Bonjour ' + @FullName + ', Nouvelle alerte: ' + @TitreAlerte + '. Description: ' + @DescriptionAlerte + '" -SmtpServer "smtp.gmail.com" -Port 587 -UseSsl -Credential (New-Object System.Management.Automation.PSCredential("khalilouerghemmi@gmail.com", (ConvertTo-SecureString "xiczhnsf ywjqwgvd" -AsPlainText -Force)))';
        
        -- Exécuter PowerShell
        DECLARE @CMD NVARCHAR(MAX);
        SET @CMD = 'powershell.exe -Command "' + @PSScript + '"';
        
        BEGIN TRY
            EXEC xp_cmdshell @CMD;
            PRINT 'TRIGGER: Email envoyé à ' + @Email;
            
            -- Marquer comme envoyé
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Envoyé par Email'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
            
        END TRY
        BEGIN CATCH
            PRINT 'TRIGGER: Erreur envoi email';
        END CATCH
    END
    
    PRINT 'TRIGGER: Traitement terminé pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
END;
GO

PRINT 'Trigger TR_Alerte_Simple_Email créé avec succès !';
PRINT 'Test avec: INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId) VALUES (2, 1, 1, 2, ''Test Email'', ''Test envoi réel'', GETDATE(), 1, 2, 1, 1);';
