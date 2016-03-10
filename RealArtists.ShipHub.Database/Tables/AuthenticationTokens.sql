﻿CREATE TABLE [dbo].[AuthenticationTokens] (
  [Token]          UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL,
  [AccountId]      INT              NOT NULL,
  [ClientName]     NVARCHAR (150)   NOT NULL,
  [CreationDate]   DATETIMEOFFSET   NOT NULL,
  [LastAccessDate] DATETIMEOFFSET   NOT NULL,
  CONSTRAINT [PK_AuthenticationTokens] PRIMARY KEY CLUSTERED ([Token] ASC),
  CONSTRAINT [FKCD_AuthenticationTokens_AccountId_Accounts_Id] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AuthenticationTokens_AccountId] ON [dbo].[AuthenticationTokens]([AccountId] ASC);
GO
