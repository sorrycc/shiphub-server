﻿CREATE TABLE [dbo].[Comments] (
  [Id]           INT            NOT NULL,
  [IssueId]      INT            NOT NULL,
  [RepositoryId] INT            NOT NULL,
  [UserId]       INT            NOT NULL,
  [Body]         NVARCHAR(MAX)  NOT NULL,
  [CreatedAt]    DATETIMEOFFSET NOT NULL,
  [UpdatedAt]    DATETIMEOFFSET NOT NULL,
  [Reactions]    NVARCHAR(300)  NULL,
  [RowVersion]   BIGINT         NULL,
  CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED ([Id]),
  CONSTRAINT [FK_Comments_IssueId_Issues_Id] FOREIGN KEY ([IssueId]) REFERENCES [dbo].[Issues]([Id]),
  CONSTRAINT [FK_Comments_RepositoryId_Repositories_Id] FOREIGN KEY ([RepositoryId]) REFERENCES [dbo].[Repositories]([Id]),
  CONSTRAINT [FK_Comments_UserId_Accounts_Id] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Accounts]([Id]),
);
GO

CREATE NONCLUSTERED INDEX [IX_Comments_IssueId] ON [dbo].[Comments]([IssueId]);
GO

CREATE NONCLUSTERED INDEX [IX_Comments_RepositoryId] ON [dbo].[Comments]([RepositoryId]);
GO

CREATE NONCLUSTERED INDEX [IX_Comments_UserId] ON [dbo].[Comments]([UserId]);
GO

CREATE NONCLUSTERED INDEX [IX_Comments_RowVersion] ON [dbo].[Comments]([RowVersion]);
GO

CREATE TRIGGER [dbo].[TRG_Comments_Version]
ON [dbo].[Comments]
AFTER INSERT, UPDATE
NOT FOR REPLICATION
AS 
BEGIN
  -- SET NOCOUNT ON added to prevent extra result sets from
  -- interfering with SELECT statements.
  SET NOCOUNT ON;

  UPDATE Comments SET
    [RowVersion] = NEXT VALUE FOR [dbo].[SyncIdentifier]
  WHERE Id IN (SELECT Id FROM inserted)
END
GO