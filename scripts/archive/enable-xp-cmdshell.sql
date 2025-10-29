-- Activer xp_cmdshell pour permettre l'exÃ©cution de PowerShell depuis SQL Server
-- Ã€ exÃ©cuter en tant qu'administrateur

USE master;
GO

-- Activer les options avancÃ©es
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

-- Activer xp_cmdshell
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;

PRINT 'xp_cmdshell activÃ© avec succÃ¨s !';
PRINT 'Maintenant SQL Server peut exÃ©cuter des commandes PowerShell pour envoyer des emails.';

