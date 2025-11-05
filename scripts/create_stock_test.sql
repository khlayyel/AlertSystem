/*
  Create a simple StockItem table in AlertDB for testing
  Safe to run multiple times.
*/

IF DB_ID(N'AlertDB') IS NULL BEGIN
  PRINT 'Database AlertDB not found. Run create_alert_db.sql first.';
  RETURN;
END
GO

USE [AlertDB]
GO

IF OBJECT_ID(N'dbo.StockItem', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.StockItem
  (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ArticleCode NVARCHAR(64) NOT NULL,
    NomProduit NVARCHAR(256) NOT NULL,
    Quantity INT NOT NULL,
    MinQty INT NOT NULL,
    MaxQty INT NOT NULL,
    LastUpdated DATETIME2 NOT NULL CONSTRAINT DF_StockItem_LastUpdated DEFAULT (SYSUTCDATETIME())
  );
END
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.StockItem)
BEGIN
  INSERT INTO dbo.StockItem (ArticleCode, NomProduit, Quantity, MinQty, MaxQty)
  VALUES
    (N'A1', N'Article A1', 2, 5, 15),   -- below min
    (N'A2', N'Article A2', 6, 5, 10),   -- normal
    (N'A3', N'Article A3', 12, 3, 10);  -- above max
END

PRINT 'StockItem test data ready.';
GO

