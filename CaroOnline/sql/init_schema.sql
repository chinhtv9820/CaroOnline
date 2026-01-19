IF DB_ID('CaroDb') IS NULL
BEGIN
    CREATE DATABASE CaroDb;
END
GO

USE CaroDb;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
  -- Create Users table with Email column
  CREATE TABLE dbo.Users (
    Id INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    DisplayName NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
  );
  CREATE INDEX IX_Users_Email ON dbo.Users(Email);
END
ELSE
BEGIN
  -- Add Email column if it doesn't exist (for existing databases)
  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
  BEGIN
    PRINT 'Adding Email column to existing Users table...';
  END
  ELSE
  BEGIN
    PRINT 'Email column already exists.';
  END
END
GO

-- Step 1: Add Email column as nullable (must be in separate batch)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
  AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
  ALTER TABLE dbo.Users ADD Email NVARCHAR(255) NULL;
  PRINT 'Step 1: Email column added (nullable)';
END
GO

-- Step 2: Update existing rows with placeholder email (must be in separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
BEGIN
  UPDATE dbo.Users 
  SET Email = Username + '@example.com' 
  WHERE Email IS NULL;
  PRINT 'Step 2: Updated existing users with placeholder emails';
END
GO

-- Step 3: Make Email NOT NULL (must be in separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
  AND (SELECT is_nullable FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email') = 1
BEGIN
  ALTER TABLE dbo.Users ALTER COLUMN Email NVARCHAR(255) NOT NULL;
  PRINT 'Step 3: Email column set to NOT NULL';
END
GO

-- Step 4: Create unique index on Email (must be in separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Email')
  AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
  CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
  PRINT 'Step 4: Unique index created on Email';
END
GO

IF OBJECT_ID('dbo.Games', 'U') IS NULL
BEGIN
CREATE TABLE dbo.Games (
  Id INT IDENTITY PRIMARY KEY,
  CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  StartTime DATETIME2 NULL,
  FinishedAt DATETIME2 NULL,
  Result NVARCHAR(50) NULL,
  Mode NVARCHAR(20) NOT NULL,
  P1UserId INT NULL,
  P2UserId INT NULL,
  WinnerId INT NULL,
  IsAi BIT DEFAULT 0,
  AiLevel INT NULL,
  WinType NVARCHAR(50) NULL,
  PveDifficulty INT NULL,
  TimeControlSeconds INT NOT NULL,
  CurrentTurn INT DEFAULT 1,
  RoomOwnerId INT NULL,
  CONSTRAINT FK_Games_P1 FOREIGN KEY (P1UserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_Games_P2 FOREIGN KEY (P2UserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_Games_Winner FOREIGN KEY (WinnerId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_Games_Owner FOREIGN KEY (RoomOwnerId) REFERENCES dbo.Users(Id)
);
CREATE INDEX IX_Games_WinnerId ON dbo.Games(WinnerId);
END
GO

IF OBJECT_ID('dbo.Moves', 'U') IS NULL
BEGIN
CREATE TABLE dbo.Moves (
  Id INT IDENTITY PRIMARY KEY,
  GameId INT NOT NULL,
  Player INT NOT NULL,
  X INT NOT NULL,
  Y INT NOT NULL,
  MoveNumber INT NOT NULL,
  CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Moves_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(Id)
);
END
GO

IF OBJECT_ID('dbo.GameHistory', 'U') IS NULL
BEGIN
CREATE TABLE dbo.GameHistory (
  Id INT IDENTITY PRIMARY KEY,
  GameId INT NOT NULL,
  Event NVARCHAR(200) NOT NULL,
  CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_History_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(Id)
);
END
GO

IF OBJECT_ID('dbo.Messages', 'U') IS NULL
BEGIN
CREATE TABLE dbo.Messages (
  Id INT IDENTITY PRIMARY KEY,
  GameId INT NOT NULL,
  SenderId INT NOT NULL,
  Content NVARCHAR(500) NOT NULL,
  Timestamp DATETIME2 DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Messages_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(Id),
  CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderId) REFERENCES dbo.Users(Id)
);
CREATE INDEX IX_Messages_GameId ON dbo.Messages(GameId);
CREATE INDEX IX_Messages_SenderId ON dbo.Messages(SenderId);
END
GO

IF OBJECT_ID('dbo.Rankings', 'U') IS NULL
BEGIN
CREATE TABLE dbo.Rankings (
  Id INT IDENTITY PRIMARY KEY,
  UserId INT NOT NULL,
  PeriodType NVARCHAR(20) NOT NULL,
  Rank INT NOT NULL,
  Score INT NOT NULL,
  PeriodStart DATETIME2 NOT NULL,
  PeriodEnd DATETIME2 NOT NULL,
  CONSTRAINT FK_Rankings_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
CREATE INDEX IX_Rankings_User_Period ON dbo.Rankings(UserId, PeriodType, PeriodStart);
END
GO


