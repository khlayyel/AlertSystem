-- Supprimer la colonne IsRead qui n'est plus utilisée
USE [AlertSystemDB]
GO

-- Supprimer l'index qui utilise IsRead s'il existe
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertRecipients_UserId_IsRead_IsConfirmed')
BEGIN
    DROP INDEX IX_AlertRecipients_UserId_IsRead_IsConfirmed ON AlertRecipients
    PRINT 'Index IX_AlertRecipients_UserId_IsRead_IsConfirmed supprimé'
END

-- Supprimer la colonne IsRead
ALTER TABLE AlertRecipients DROP COLUMN IsRead
PRINT 'Colonne IsRead supprimée'
GO
