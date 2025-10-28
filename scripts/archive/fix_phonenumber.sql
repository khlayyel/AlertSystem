-- Script pour corriger le type PhoneNumber
USE [AlertSystemDB]
GO

-- Vérifier le type actuel de PhoneNumber
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PhoneNumber'
GO

-- Modifier le type de PhoneNumber de int vers nvarchar
ALTER TABLE Users ALTER COLUMN PhoneNumber NVARCHAR(20) NULL
GO

PRINT 'PhoneNumber column type updated to NVARCHAR(20)'
GO
