-- Activer xp_cmdshell pour permettre l'exécution de PowerShell depuis SQL Server
-- À exécuter en tant qu'administrateur

USE master;
GO

-- Activer les options avancées
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

-- Activer xp_cmdshell
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;

PRINT 'xp_cmdshell activé avec succès !';
PRINT 'Maintenant SQL Server peut exécuter des commandes PowerShell pour envoyer des emails.';
