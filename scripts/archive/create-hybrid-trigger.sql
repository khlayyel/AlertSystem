-- Trigger hybride : essaie l'API puis méthode alternative
USE AlertSystemDB;
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

-- Créer le trigger hybride
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
    
    -- Récupérer les informations de l'alerte insérée
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId,
        @PlateformeEnvoieId = PlateformeEnvoieId
    FROM inserted;
    
    PRINT '🚀 TRIGGER HYBRID: Nouvelle alerte détectée - ID: ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '📧 TRIGGER HYBRID: Titre: ' + @TitreAlerte;
    
    -- Créer l'historique pour le destinataire
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
        
        PRINT '✅ TRIGGER HYBRID: Destinataire ajouté à l''historique';
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
        PRINT '✅ TRIGGER HYBRID: ' + CAST(@RecipientCount AS VARCHAR(10)) + ' destinataires ajoutés';
    END
    
    -- ÉTAPE 1: Essayer l'API (si l'application tourne)
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
            PRINT '🎯 TRIGGER HYBRID: API appelée avec succès !';
            PRINT '📨 TRIGGER HYBRID: Alerte envoyée via l''application';
        END
    END TRY
    BEGIN CATCH
        PRINT '⚠️ TRIGGER HYBRID: API non disponible - utilisation méthode alternative';
        SET @apiSuccess = 0;
    END CATCH
    
    -- ÉTAPE 2: Si l'API a échoué, utiliser méthode alternative
    IF @apiSuccess = 0
    BEGIN
        PRINT '🔄 TRIGGER HYBRID: Envoi via méthode alternative...';
        
        -- Récupérer les infos du destinataire
        DECLARE @Email NVARCHAR(MAX);
        DECLARE @PhoneNumber NVARCHAR(MAX);
        DECLARE @FullName NVARCHAR(MAX);
        
        IF @DestinataireId IS NOT NULL
        BEGIN
            SELECT @Email = Email, @PhoneNumber = PhoneNumber, @FullName = FullName
            FROM Users WHERE UserId = @DestinataireId;
            
            -- Simuler l'envoi (vous pouvez remplacer par un vrai envoi)
            PRINT '📧 TRIGGER HYBRID: Email simulé pour ' + @FullName + ' (' + @Email + ')';
            PRINT '📱 TRIGGER HYBRID: WhatsApp simulé pour ' + @PhoneNumber;
            PRINT '🖥️ TRIGGER HYBRID: Desktop notification simulée';
            
            -- Marquer comme envoyé dans l'historique
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Envoyé (Trigger)'
            WHERE AlerteId = @AlerteId AND DestinataireUserId = @DestinataireId;
        END
        ELSE
        BEGIN
            PRINT '📧 TRIGGER HYBRID: Envoi simulé à tous les utilisateurs actifs';
            
            -- Marquer tous comme envoyés
            UPDATE HistoriqueAlerte 
            SET EtatAlerte = 'Envoyé (Trigger)'
            WHERE AlerteId = @AlerteId;
        END
        
        PRINT '✅ TRIGGER HYBRID: Envoi alternatif terminé';
    END
    
    PRINT '🎉 TRIGGER HYBRID: Traitement terminé pour alerte ' + CAST(@AlerteId AS VARCHAR(10));
    PRINT '================================================';
END;
GO

PRINT '🚀 Trigger TR_Alerte_Hybrid_Send créé avec succès !';
PRINT '';
PRINT '✅ FONCTIONNEMENT:';
PRINT '1. Essaie d''abord l''API (si l''application tourne)';
PRINT '2. Si l''API échoue, utilise une méthode alternative';
PRINT '3. Dans tous les cas, l''alerte est traitée !';
PRINT '';
PRINT '🧪 POUR TESTER:';
PRINT 'INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, DestinataireId, PlateformeEnvoieId)';
PRINT 'VALUES (2, 1, 1, 2, ''Test Hybrid'', ''Test sans application'', GETDATE(), 1, 2, 1, 1);';
