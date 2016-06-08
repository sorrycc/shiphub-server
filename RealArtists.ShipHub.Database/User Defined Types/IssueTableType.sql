﻿CREATE TYPE [dbo].[IssueTableType] AS TABLE (
  [Id]           BIGINT         NOT NULL PRIMARY KEY CLUSTERED,
  [UserId]       BIGINT         NOT NULL,
  [Number]       INT            NOT NULL,
  [State]        NVARCHAR(6)    NOT NULL,
  [Title]        NVARCHAR(255)  NOT NULL,
  [Body]         NVARCHAR(MAX)  NULL,
  [AssigneeId]   BIGINT         NULL,
  [MilestoneId]  BIGINT         NULL,
  [Locked]       BIT            NOT NULL,
  [CreatedAt]    DATETIMEOFFSET NOT NULL,
  [UpdatedAt]    DATETIMEOFFSET NOT NULL,
  [ClosedAt]     DATETIMEOFFSET NULL,
  [ClosedById]   BIGINT         NULL,
  [Reactions]    NVARCHAR(300)  NULL
)