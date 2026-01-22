-- Quick fix script to add Email column to existing Users table
-- Run this if you get "Invalid column name 'Email'" error
-- IMPORTANT: Each step must be in a separate batch (separated by GO)

USE CaroDb;
GO

-- Check if Email column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
    PRINT 'Adding Email column to Users table...';
END
ELSE
BEGIN
    PRINT 'Email column already exists. No changes needed.';
END
GO

-- Step 1: Add Email column as nullable first (MUST be separate batch)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
    ALTER TABLE dbo.Users ADD Email NVARCHAR(255) NULL;
    PRINT 'Step 1: Email column added (nullable)';
END
GO

-- Step 2: Update existing rows with placeholder email (MUST be separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
    UPDATE dbo.Users 
    SET Email = Username + '@example.com' 
    WHERE Email IS NULL;
    PRINT 'Step 2: Updated existing users with placeholder emails';
END
GO

-- Step 3: Make Email NOT NULL (MUST be separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN Email NVARCHAR(255) NOT NULL;
    PRINT 'Step 3: Email column set to NOT NULL';
END
GO

-- Step 4: Create unique index on Email (MUST be separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
  AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
    PRINT 'Step 4: Unique index created on Email';
END
GO

PRINT 'SUCCESS: Email column migration completed!';
PRINT 'NOTE: Existing users have placeholder emails. Please update them manually if needed.';
GO

