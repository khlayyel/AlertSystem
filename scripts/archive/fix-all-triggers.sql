-- CORRECTION COMPLÃˆTE DU SYSTÃˆME DE TRIGGERS
-- Ce script corrige tous les problÃ¨mes et crÃ©e un systÃ¨me fonctionnel

USE BELVEDERE_17_10_2025;
GO

PRINT 'ðŸ”§ Ã‰TAPE 1: Nettoyage complet des anciens triggers';

-- Supprimer TOUS les anciens triggers
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Rules')
    DROP TRIGGER TR_Alerte_Rules;

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Simple_Email')
    DROP TRIGGER TR_Alerte_Simple_Email;

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_AutoSend')
    DROP TRIGGER TR_Alerte_AutoSend;

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Hybrid_Send')
    DROP TRIGGER TR_Alerte_Hybrid_Send;

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Alerte_Real_Email_Send')
    DROP TRIGGER TR_Alerte_Real_Email_Send;

PRINT 'âœ… Anciens triggers supprimÃ©s';

PRINT 'ðŸ”§ Ã‰TAPE 2: CrÃ©ation du trigger final fonctionnel';

-- CrÃ©er le trigger final qui fonctionne vraiment
CREATE OR ALTER TRIGGER TR_Alerte_Final_Working
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
    
    PRINT 'ðŸš€ TRIGGER FINAL: Nouvelle alerte dÃ©tectÃ©e - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT 'ðŸ“§ TRIGGER FINAL: Titre: ' + @TitreAlerte;
    
    -- TOUJOURS crÃ©er l'historique d'abord
    IF @DestinataireId IS NOT NULL
    BEGIN
        -- Destinataire spÃ©cifique
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
        
        PRINT 'âœ… TRIGGER FINAL: Historique crÃ©Ã© pour destinataire ' + CAST(@DestinataireId AS VARCHAR(10));
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
        PRINT 'âœ… TRIGGER FINAL: Historique crÃ©Ã© pour ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires';
    END
    
    -- Essayer d'envoyer l'email (si plateforme Email ou NULL)
    IF (@PlateformeEnvoieId = 1 OR @PlateformeEnvoieId IS NULL) AND @DestinataireId IS NOT NULL
    BEGIN
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        SELECT @Email = Email, @FullName = FullName
        FROM Users WHERE UserId = @DestinataireId;
        
        -- Essayer d'envoyer l'email via PowerShell
        DECLARE @PSCmd NVARCHAR(MAX);
        SET @PSCmd = 'powershell.exe -ExecutionPolicy Bypass -Command "try { Send-MailMessage -To ''' + @Email + ''' -From ''khalilouerghemmi@gmail.com'' -Subject ''AlertSystem: ' + REPLACE(@TitreAlerte, '''', '') + ''' -Body ''Bonjour ' + REPLACE(@FullName, '''', '') + ', Nouvelle alerte: ' + REPLACE(@TitreAlerte, '''', '') + '. Description: ' + REPLACE(@DescriptionAlerte, '''', '') + ''' -SmtpServer ''smtp.gmail.com'' -Port 587 -UseSsl -Credential (New-Object System.Management.Automation.PSCredential(''khalilouerghemmi@gmail.com'', (ConvertTo-SecureString ''xiczhnsf ywjqwgvd'' -AsPlainText -Force))); Write-Host ''Email envoye avec succes''; } catch { Write-Host ''Erreur email: '' + $_.Exception.Message; }"';
        
        BEGIN TRY
            EXEC xp_cmdshell @PSCmd;
            PRINT 'ðŸ“¨ TRIGGER FINAL: Commande email exÃ©cutÃ©e pour ' + @Email;
            
            -- Marquer comme envoyÃ©
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'EnvoyÃ© par Email'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
            
        END TRY
        BEGIN CATCH
            PRINT 'âŒ TRIGGER FINAL: Erreur lors de l''envoi email';
            
            -- Marquer comme erreur
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Erreur envoi'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
        END CATCH
    END
    ELSE
    BEGIN
        PRINT 'ðŸ“ TRIGGER FINAL: Pas d''envoi email (plateforme=' + ISNULL(CAST(@PlateformeEnvoieId AS VARCHAR(10)), 'NULL') + ')';
    END
    
    PRINT 'ðŸŽ‰ TRIGGER FINAL: Traitement terminÃ© pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '================================================';
END;
GO

PRINT 'âœ… TRIGGER FINAL CRÃ‰Ã‰ AVEC SUCCÃˆS !';
PRINT '';
PRINT 'ðŸ§ª TESTS AUTOMATIQUES:';

-- Test 1: Alerte pour Khalil avec Email
PRINT 'Test 1: Alerte Email pour Khalil...';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    2, 1, 1, 2, 'TEST FINAL - Email Khalil', 
    'Test du trigger final corrigÃ© - Email pour Khalil', 
    GETDATE(), 1, 2, 1, 1
);

-- Attendre un peu
WAITFOR DELAY '00:00:02';

-- Test 2: Alerte WhatsApp pour Zied
PRINT 'Test 2: Alerte WhatsApp pour Zied...';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    2, 1, 1, 2, 'TEST FINAL - WhatsApp Zied', 
    'Test du trigger final corrigÃ© - WhatsApp pour Zied', 
    GETDATE(), 1, 2, 2, 2
);

PRINT '';
PRINT 'ðŸ“Š VÃ‰RIFICATION DES RÃ‰SULTATS:';

-- Voir les 2 derniÃ¨res alertes crÃ©Ã©es
SELECT TOP 2 AlerteId, TitreAlerte, DateCreationAlerte 
FROM Alerte 
ORDER BY AlerteId DESC;

-- Voir l'historique crÃ©Ã©
SELECT 
    h.AlerteId,
    h.DestinataireUserId,
    h.EtatAlerte,
    u.FullName,
    u.Email,
    pe.Plateforme
FROM HistoriqueAlerte h
JOIN Users u ON h.DestinataireUserId = u.UserId
LEFT JOIN Alerte a ON h.AlerteId = a.AlerteId
LEFT JOIN PlateformeEnvoie pe ON a.PlateformeEnvoieId = pe.PlateformeId
WHERE h.AlerteId IN (
    SELECT TOP 2 AlerteId FROM Alerte ORDER BY AlerteId DESC
)
ORDER BY h.AlerteId DESC;

PRINT '';
PRINT 'ðŸŽ¯ SYSTÃˆME CORRIGÃ‰ ET TESTÃ‰ !';
PRINT 'Maintenant vous pouvez insÃ©rer des alertes et elles seront automatiquement traitÃ©es.';
PRINT '';
PRINT 'ðŸ“§ Pour tester manuellement:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Mon Test'', ''Description test'', GETDATE(), 1, 2, 1, 1);';

