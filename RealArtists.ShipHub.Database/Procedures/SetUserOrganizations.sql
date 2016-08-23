﻿CREATE PROCEDURE [dbo].[SetUserOrganizations]
  @UserId BIGINT,
  @OrganizationIds ItemListTableType READONLY
AS
BEGIN
  -- SET NOCOUNT ON added to prevent extra result sets from
  -- interfering with SELECT statements.
  SET NOCOUNT ON

  DECLARE @Changes BIT = 0

  MERGE INTO AccountOrganizations WITH (UPDLOCK SERIALIZABLE) as [Target]
  USING (SELECT [Item] as [OrganizationId] FROM @OrganizationIds) as [Source]
  ON ([Target].[UserId] = @UserId  AND [Target].[OrganizationId] = [Source].[OrganizationId])
  -- Add
  WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserId], [OrganizationId])
    VALUES (@UserId, [OrganizationId])
  -- Delete
  WHEN NOT MATCHED BY SOURCE AND [Target].[UserId] = @UserId
    THEN DELETE;

  IF(@@ROWCOUNT > 0) -- Not a NOP
  BEGIN
    SET @Changes = 1
  END

  -- Return updated user
  SELECT NULL as OrganizationId, NULL as RepositoryId, @UserId as UserId WHERE @Changes = 1
END
