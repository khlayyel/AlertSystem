-- Supprimer la colonne IsRead de maniÃ¨re sÃ©curisÃ©e
USE [BELVEDERE_17_10_2025]
GO

-- Trouver et supprimer la contrainte par dÃ©faut sur IsRead
DECLARE @ConstraintName NVARCHAR(200)
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
INNER JOIN sys.tables t ON dc.parent_object_id = t.object_id
WHERE t.name = 'AlertRecipients' AND c.name = 'IsRead'

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE AlertRecipients DROP CONSTRAINT ' + @ConstraintName)
    PRINT 'Contrainte par dÃ©faut supprimÃ©e: ' + @ConstraintName
END

-- Supprimer la colonne IsRead
ALTER TABLE AlertRecipients DROP COLUMN IsRead
PRINT 'Colonne IsRead supprimÃ©e avec succÃ¨s'
GO

