-- TRIGGER SIMPLE ET FONCTIONNEL
USE AlertSystemDB;
GO

-- Supprimer tous les anciens triggers
DROP TRIGGER IF EXISTS TR_Alerte_Rules;
DROP TRIGGER IF EXISTS TR_Alerte_Simple_Email;
DROP TRIGGER IF EXISTS TR_Alerte_AutoSend;
DROP TRIGGER IF EXISTS TR_Alerte_Hybrid_Send;
DROP TRIGGER IF EXISTS TR_Alerte_Real_Email_Send;
DROP TRIGGER IF EXISTS TR_Alerte_Final_Working;
GO

-- Créer le trigger simple qui fonctionne
CREATE TRIGGER TR_Alerte_Working
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
    
    SELECT 
        @AlerteId = AlerteId,
        @TitreAlerte = TitreAlerte,
        @DescriptionAlerte = DescriptionAlerte,
        @AlertTypeId = AlertTypeId,
        @DestinataireId = DestinataireId
    FROM inserted;
    
    PRINT 'TRIGGER: Alerte ' + CAST(@AlerteId AS VARCHAR(10)) + ' - ' + @TitreAlerte;
    
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
        
        PRINT 'TRIGGER: Historique créé pour utilisateur ' + CAST(@DestinataireId AS VARCHAR(10));
    END
    ELSE
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
        WHERE u.IsActive = 1;
        
        PRINT 'TRIGGER: Historique créé pour tous les utilisateurs';
    END
    
    PRINT 'TRIGGER: Traitement terminé';
END;
GO

PRINT 'Trigger TR_Alerte_Working créé avec succès !';
PRINT 'Test maintenant avec une insertion...';
