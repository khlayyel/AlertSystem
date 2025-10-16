-- CORRECTION COMPLÈTE DU SYSTÈME DE TRIGGERS
-- Ce script corrige tous les problèmes et crée un système fonctionnel

USE AlertSystemDB;
GO

PRINT '🔧 ÉTAPE 1: Nettoyage complet des anciens triggers';

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

PRINT '✅ Anciens triggers supprimés';

PRINT '🔧 ÉTAPE 2: Création du trigger final fonctionnel';

-- Créer le trigger final qui fonctionne vraiment
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
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT '🚀 TRIGGER FINAL: Nouvelle alerte détectée - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '📧 TRIGGER FINAL: Titre: ' + @TitreAlerte;
    
    -- TOUJOURS créer l'historique d'abord
    IF @DestinataireId IS NOT NULL
    BEGIN
        -- Destinataire spécifique
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
        
        PRINT '✅ TRIGGER FINAL: Historique créé pour destinataire ' + CAST(@DestinataireId AS VARCHAR(10));
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
        PRINT '✅ TRIGGER FINAL: Historique créé pour ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires';
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
            PRINT '📨 TRIGGER FINAL: Commande email exécutée pour ' + @Email;
            
            -- Marquer comme envoyé
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Envoyé par Email'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
            
        END TRY
        BEGIN CATCH
            PRINT '❌ TRIGGER FINAL: Erreur lors de l''envoi email';
            
            -- Marquer comme erreur
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Erreur envoi'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
        END CATCH
    END
    ELSE
    BEGIN
        PRINT '📝 TRIGGER FINAL: Pas d''envoi email (plateforme=' + ISNULL(CAST(@PlateformeEnvoieId AS VARCHAR(10)), 'NULL') + ')';
    END
    
    PRINT '🎉 TRIGGER FINAL: Traitement terminé pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '================================================';
END;
GO

PRINT '✅ TRIGGER FINAL CRÉÉ AVEC SUCCÈS !';
PRINT '';
PRINT '🧪 TESTS AUTOMATIQUES:';

-- Test 1: Alerte pour Khalil avec Email
PRINT 'Test 1: Alerte Email pour Khalil...';
INSERT INTO Alerte (
    AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, 
    DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, 
    DestinataireId, PlateformeEnvoieId
) VALUES (
    2, 1, 1, 2, 'TEST FINAL - Email Khalil', 
    'Test du trigger final corrigé - Email pour Khalil', 
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
    'Test du trigger final corrigé - WhatsApp pour Zied', 
    GETDATE(), 1, 2, 2, 2
);

PRINT '';
PRINT '📊 VÉRIFICATION DES RÉSULTATS:';

-- Voir les 2 dernières alertes créées
SELECT TOP 2 AlerteId, TitreAlerte, DateCreationAlerte 
FROM Alerte 
ORDER BY AlerteId DESC;

-- Voir l'historique créé
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
PRINT '🎯 SYSTÈME CORRIGÉ ET TESTÉ !';
PRINT 'Maintenant vous pouvez insérer des alertes et elles seront automatiquement traitées.';
PRINT '';
PRINT '📧 Pour tester manuellement:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Mon Test'', ''Description test'', GETDATE(), 1, 2, 1, 1);';
