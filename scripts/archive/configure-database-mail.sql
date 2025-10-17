-- Configuration de Database Mail pour envoi d'emails direct depuis SQL Server
-- À exécuter en tant qu'administrateur SQL Server

USE msdb;
GO

-- Activer Database Mail
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'Database Mail XPs', 1;
RECONFIGURE;

-- Créer un profil de messagerie
IF NOT EXISTS (SELECT * FROM msdb.dbo.sysmail_profile WHERE name = 'AlertSystem')
BEGIN
    EXEC msdb.dbo.sysmail_add_profile_sp
        @profile_name = 'AlertSystem',
        @description = 'Profil pour AlertSystem notifications';
    
    PRINT 'Profil AlertSystem créé';
END
ELSE
    PRINT 'Profil AlertSystem existe déjà';

-- Créer un compte de messagerie (Gmail SMTP)
IF NOT EXISTS (SELECT * FROM msdb.dbo.sysmail_account WHERE name = 'AlertSystem_Gmail')
BEGIN
    EXEC msdb.dbo.sysmail_add_account_sp
        @account_name = 'AlertSystem_Gmail',
        @description = 'Compte Gmail pour AlertSystem',
        @email_address = 'khalilouerghemmi@gmail.com',
        @display_name = 'AlertSystem',
        @mailserver_name = 'smtp.gmail.com',
        @port = 587,
        @enable_ssl = 1,
        @username = 'khalilouerghemmi@gmail.com',
        @password = 'VOTRE_MOT_DE_PASSE_APP_GMAIL'; -- À remplacer par votre mot de passe d'application
    
    PRINT 'Compte Gmail créé';
END
ELSE
    PRINT 'Compte Gmail existe déjà';

-- Associer le compte au profil
IF NOT EXISTS (SELECT * FROM msdb.dbo.sysmail_profileaccount pa 
               INNER JOIN msdb.dbo.sysmail_profile p ON pa.profile_id = p.profile_id
               INNER JOIN msdb.dbo.sysmail_account a ON pa.account_id = a.account_id
               WHERE p.name = 'AlertSystem' AND a.name = 'AlertSystem_Gmail')
BEGIN
    EXEC msdb.dbo.sysmail_add_profileaccount_sp
        @profile_name = 'AlertSystem',
        @account_name = 'AlertSystem_Gmail',
        @sequence_number = 1;
    
    PRINT 'Compte associé au profil';
END
ELSE
    PRINT 'Compte déjà associé au profil';

-- Donner les permissions au profil
EXEC msdb.dbo.sysmail_add_principalprofile_sp
    @profile_name = 'AlertSystem',
    @principal_name = 'public',
    @is_default = 1;

PRINT '';
PRINT '=== CONFIGURATION DATABASE MAIL TERMINÉE ===';
PRINT '';
PRINT 'IMPORTANT: Remplacez VOTRE_MOT_DE_PASSE_APP_GMAIL par votre vrai mot de passe d''application Gmail';
PRINT '';
PRINT 'Pour tester l''envoi d''email :';
PRINT 'EXEC msdb.dbo.sp_send_dbmail';
PRINT '    @profile_name = ''AlertSystem'',';
PRINT '    @recipients = ''khalilouerghemmi@gmail.com'',';
PRINT '    @subject = ''Test Database Mail'',';
PRINT '    @body = ''Test envoi depuis SQL Server'';';
