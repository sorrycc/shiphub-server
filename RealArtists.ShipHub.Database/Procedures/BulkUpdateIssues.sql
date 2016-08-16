﻿CREATE PROCEDURE [dbo].[BulkUpdateIssues]
  @RepositoryId BIGINT,
  @Issues IssueTableType READONLY,
  @Labels LabelTableType READONLY,
  @Assignees MappingTableType READONLY
AS
BEGIN
  -- SET NOCOUNT ON added to prevent extra result sets from
  -- interfering with SELECT statements.
  SET NOCOUNT ON

  -- For tracking required updates to repo log
  DECLARE @Changes TABLE (
    [IssueId] BIGINT NOT NULL
  )

  DECLARE @UniqueChanges TABLE (
    [IssueId] BIGINT NOT NULL PRIMARY KEY CLUSTERED
  )

  MERGE INTO Issues WITH (SERIALIZABLE) as [Target]
  USING (
    SELECT [Id], [UserId], [Number], [State], [Title], [Body], [MilestoneId], [Locked], [CreatedAt], [UpdatedAt], [ClosedAt], [ClosedById], [PullRequest]
    FROM @Issues
  ) as [Source]
  ON ([Target].[Id] = [Source].[Id])
  -- Add
  WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [UserId], [RepositoryId], [Number], [State], [Title], [Body], [MilestoneId], [Locked], [CreatedAt], [UpdatedAt], [ClosedAt], [ClosedById], [PullRequest])
    VALUES ([Id], [UserId], @RepositoryId, [Number], [State], [Title], [Body], [MilestoneId], [Locked], [CreatedAt], [UpdatedAt], [ClosedAt], [ClosedById], [PullRequest])
  -- Update (this bumps for label only changes too)
  WHEN MATCHED AND [Target].[UpdatedAt] < [Source].[UpdatedAt] THEN
    UPDATE SET
      [UserId] = [Source].[UserId], -- This can change to ghost
      [State] = [Source].[State],
      [Title] = [Source].[Title],
      [Body] = [Source].[Body],
      [MilestoneId] = [Source].[MilestoneId],
      [Locked] = [Source].[Locked],
      [UpdatedAt] = [Source].[UpdatedAt],
      [ClosedAt] = [Source].[ClosedAt],
      [ClosedById] = [Source].[ClosedById],
      [PullRequest] = [Source].[PullRequest]
  OUTPUT INSERTED.Id INTO @Changes (IssueId)
  OPTION (RECOMPILE);

  EXEC [dbo].[BulkCreateLabels] @Labels = @Labels

  MERGE INTO IssueLabels WITH (SERIALIZABLE) as [Target]
  USING (
    SELECT L1.Id as LabelId, L2.ItemId as IssueId
    FROM Labels as L1
      INNER JOIN @Labels as L2 ON (L1.Color = L2.Color AND L1.Name = L2.Name)
  ) as [Source]
  ON ([Target].LabelId = [Source].LabelId AND [Target].IssueId = [Source].IssueId)
  -- Add
  WHEN NOT MATCHED BY TARGET THEN
    INSERT (IssueId, LabelId)
    VALUES (IssueId, LabelId)
  -- Delete
  WHEN NOT MATCHED BY SOURCE
    AND [Target].IssueId IN (SELECT DISTINCT(ItemId) FROM @Labels)
    THEN DELETE
  OPTION (RECOMPILE);

  -- Assignees
  MERGE INTO IssueAssignees WITH(SERIALIZABLE) as [Target]
  USING (
    SELECT Item1 as IssueId, Item2 as UserId FROM @Assignees
  ) as [Source]
  ON ([Target].IssueId = [Source].IssueId AND [Target].UserId = [Source].UserId)
  -- Add
  WHEN NOT MATCHED BY TARGET THEN
    INSERT (IssueId, UserId)
    VALUES (IssueId, UserId)
  -- Delete
  WHEN NOT MATCHED BY SOURCE
    AND [Target].IssueId IN (SELECT DISTINCT(Id) FROM @Issues)
    THEN DELETE
  OUTPUT ISNULL(INSERTED.IssueId, DELETED.IssueId) INTO @Changes (IssueId)
  OPTION (RECOMPILE);

  INSERT INTO @UniqueChanges (IssueId)
  SELECT DISTINCT(IssueId) FROM @Changes

  -- Update existing issues
  UPDATE RepositoryLog WITH (SERIALIZABLE) SET
    [RowVersion] = DEFAULT
  FROM RepositoryLog as rl
    INNER JOIN @UniqueChanges as c ON (rl.ItemId = c.IssueId)
  WHERE RepositoryId = @RepositoryId AND [Type] = 'issue'
  OPTION (RECOMPILE)

  -- New issues
  INSERT INTO RepositoryLog WITH (SERIALIZABLE) (RepositoryId, [Type], ItemId, [Delete])
  SELECT @RepositoryId, 'issue', c.IssueId, 0
  FROM @UniqueChanges as c
  WHERE NOT EXISTS (SELECT * FROM RepositoryLog WHERE ItemId = c.IssueId AND RepositoryId = @RepositoryId AND [Type] = 'issue')
  OPTION (RECOMPILE)

  -- Add new account references to log
  -- Removed account references are leaked or GC'd later by another process.
  MERGE INTO RepositoryLog WITH (SERIALIZABLE) as [Target]
  USING (
    SELECT Distinct(UPUserId) as UserId
    FROM Issues as c
        INNER JOIN @UniqueChanges as ch ON (c.Id = ch.IssueId)
      UNPIVOT (UPUserId FOR [Role] IN (UserId, ClosedById)) as [Ignored]
    UNION
    SELECT Item2 FROM @Assignees
  ) as [Source]
  ON ([Target].RepositoryId = @RepositoryId
    AND [Target].[Type] = 'account'
    AND [Target].ItemId = [Source].UserId)
  -- Insert
  WHEN NOT MATCHED BY TARGET THEN
    INSERT (RepositoryId, [Type], ItemId, [Delete])
    VALUES (@RepositoryId, 'account', [Source].UserId, 0)
  OPTION (RECOMPILE);

  -- Return repository if updated
  SELECT NULL as OrganizationId, @RepositoryId as RepositoryId, NULL as UserId
  WHERE EXISTS (SELECT * FROM @UniqueChanges)
  OPTION (RECOMPILE)
END
