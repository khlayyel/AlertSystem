-- Complete cleanup: Remove ALL triggers and NotificationOutbox table
USE AlertSystemDB;
GO

PRINT 'Starting complete cleanup of triggers and NotificationOutbox table...';

-- 1. Drop ALL triggers on Alerte table
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = STRING_AGG('DROP TRIGGER [' + t.name + '];', CHAR(13) + CHAR(10))
FROM sys.triggers t
WHERE t.parent_id = OBJECT_ID('dbo.Alerte');

IF @sql IS NOT NULL AND LEN(@sql) > 0
BEGIN
    PRINT 'Dropping triggers on Alerte table:';
    PRINT @sql;
    EXEC sp_executesql @sql;
    PRINT 'All triggers on Alerte table dropped.';
END
ELSE
BEGIN
    PRINT 'No triggers found on Alerte table.';
END
GO

-- 2. Drop ALL database-level triggers (if any)
DECLARE @sql2 NVARCHAR(MAX) = '';
SELECT @sql2 = STRING_AGG('DROP TRIGGER [' + t.name + '];', CHAR(13) + CHAR(10))
FROM sys.triggers t
WHERE t.parent_id = 0; -- Database-level triggers

IF @sql2 IS NOT NULL AND LEN(@sql2) > 0
BEGIN
    PRINT 'Dropping database-level triggers:';
    PRINT @sql2;
    EXEC sp_executesql @sql2;
    PRINT 'All database-level triggers dropped.';
END
ELSE
BEGIN
    PRINT 'No database-level triggers found.';
END
GO

-- 3. Drop NotificationOutbox table completely
IF OBJECT_ID('dbo.NotificationOutbox', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.NotificationOutbox;
    PRINT 'NotificationOutbox table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'NotificationOutbox table does not exist.';
END
GO

-- 4. Verify ProcessedByWorker column exists on Alerte table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alerte') AND name = 'ProcessedByWorker')
BEGIN
    ALTER TABLE dbo.Alerte ADD ProcessedByWorker BIT NOT NULL DEFAULT 0;
    CREATE INDEX IX_Alerte_ProcessedByWorker ON dbo.Alerte(ProcessedByWorker, AlerteId);
    PRINT 'Added ProcessedByWorker column and index to Alerte table.';
END
ELSE
BEGIN
    PRINT 'ProcessedByWorker column already exists on Alerte table.';
END
GO

-- 5. List remaining triggers (should be empty)
PRINT 'Remaining triggers in database:';
SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.type_desc AS TriggerType
FROM sys.triggers t
ORDER BY t.name;

-- 6. Verify cleanup
PRINT 'Cleanup verification:';
PRINT '- NotificationOutbox table: ' + CASE WHEN OBJECT_ID('dbo.NotificationOutbox', 'U') IS NULL THEN 'REMOVED' ELSE 'STILL EXISTS' END;
PRINT '- ProcessedByWorker column: ' + CASE WHEN EXISTS(SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Alerte') AND name = 'ProcessedByWorker') THEN 'EXISTS' ELSE 'MISSING' END;

PRINT 'Complete cleanup finished. System is now ready for AlertePollingWorker only.';
