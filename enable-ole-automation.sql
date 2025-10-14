-- Activer les procédures OLE Automation pour permettre les appels HTTP depuis SQL
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'Ole Automation Procedures', 1;
RECONFIGURE;

PRINT 'OLE Automation Procedures activées avec succès !';
