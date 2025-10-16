-- Supprimer la colonne IsRead de manière sécurisée
USE [AlertSystemDB]
GO

-- Trouver et supprimer la contrainte par défaut sur IsRead
DECLARE @ConstraintName NVARCHAR(200)
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
INNER JOIN sys.tables t ON dc.parent_object_id = t.object_id
WHERE t.name = 'AlertRecipients' AND c.name = 'IsRead'

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE AlertRecipients DROP CONSTRAINT ' + @ConstraintName)
    PRINT 'Contrainte par défaut supprimée: ' + @ConstraintName
END

-- Supprimer la colonne IsRead
ALTER TABLE AlertRecipients DROP COLUMN IsRead
PRINT 'Colonne IsRead supprimée avec succès'
GO
