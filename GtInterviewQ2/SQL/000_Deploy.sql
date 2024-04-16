IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'q2')
BEGIN
    EXEC('CREATE SCHEMA q2');
END
GO

drop table if exists [q2].[GroupableValue];
drop table if exists [q2].[GroupableLevel];

go


IF NOT EXISTS (SELECT * FROM sys.tables where [name] = 'GroupableLevel' and [schema_id] = SCHEMA_ID('q2'))
BEGIN

	CREATE TABLE [q2].[GroupableLevel]
	(
		[GroupableLevelId] INT NOT NULL IDENTITY(1,1),
		[ParentId] INT NULL,
		[GroupLabel] NVARCHAR(100) NULL,
		[GroupableLevelPath] hierarchyid NOT NULL,
        CONSTRAINT FK_GroupableLevel_ParentId FOREIGN KEY ([ParentId]) REFERENCES [q2].[GroupableLevel] ([GroupableLevelId]),
        CONSTRAINT PK_GroupableLevel PRIMARY KEY (GroupableLevelId),
		CONSTRAINT UX_GroupValue_ParentIdGroupLabel UNIQUE NONCLUSTERED ([ParentId], [GroupLabel])
	);

	ALTER TABLE [q2].[GroupableLevel] ADD  CONSTRAINT [DF_GroupableLevel_GroupableLevelPath] DEFAULT ([hierarchyid]::GetRoot()) FOR [GroupableLevelPath];

END

GO

IF NOT EXISTS (SELECT * FROM sys.tables where [name] = 'GroupableLevel' and [schema_id] = SCHEMA_ID('q2'))
BEGIN

	CREATE TABLE [q2].[GroupableLevel]
	(
		[GroupableLevelId] INT NOT NULL IDENTITY(1,1),
		[ParentId] INT NULL,
		[GroupLabel] NVARCHAR(100) NULL,
		[GroupableLevelPath] hierarchyid NOT NULL,
        CONSTRAINT FK_GroupableLevel_ParentId FOREIGN KEY ([ParentId]) REFERENCES [q2].[GroupableLevel] ([GroupableLevelId]),
        CONSTRAINT PK_GroupableLevel PRIMARY KEY (GroupableLevelId),
		CONSTRAINT UX_GroupValue_ParentIdGroupLabel UNIQUE NONCLUSTERED ([ParentId], [GroupLabel])
	);

	ALTER TABLE [q2].[GroupableLevel] ADD  CONSTRAINT [DF_GroupableLevel_GroupableLevelPath] DEFAULT ([hierarchyid]::GetRoot()) FOR [GroupableLevelPath];

END

GO
 
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GroupableLevel_GroupableLevelPath')
BEGIN
    CREATE NONCLUSTERED INDEX IX_GroupableLevel_GroupableLevelPath ON q2.GroupableLevel (GroupableLevelPath) INCLUDE ([GroupLabel]);
END
 
GO

CREATE OR ALTER TRIGGER [q2].[Tgr_GroupableLevel_GroupableLevelPath]
   ON [q2].[GroupableLevel] 
   AFTER INSERT,DELETE,UPDATE
AS 
BEGIN
    SET NOCOUNT ON;

    WITH Paths AS
    (
        SELECT
			GroupableLevelPath = CONVERT(hierarchyid, CONCAT(CONVERT(varchar(500), hierarchyid::GetRoot()), CAST(GroupableLevelId AS varchar(30)), '/')),
            GroupableLevelId,
            ParentId
        FROM [q2].[GroupableLevel]
        WHERE ParentId IS NULL
        UNION ALL
        SELECT
            GroupableLevelPath = CONVERT(hierarchyid, CONCAT(CONVERT(varchar(500), p.GroupableLevelPath), CAST(c.GroupableLevelId AS varchar(30)), '/')),
            c.GroupableLevelId,
            c.ParentId
        FROM [q2].[GroupableLevel] AS c
        JOIN Paths AS p ON p.GroupableLevelId = c.ParentId
    )
    UPDATE G
    SET GroupableLevelPath = CONVERT(hierarchyid, P.GroupableLevelPath)
    FROM [q2].[GroupableLevel] AS G
    JOIN Paths AS P ON G.GroupableLevelId = P.GroupableLevelId;
END;

GO

IF NOT EXISTS (SELECT * FROM sys.tables where [name] = 'GroupableValue' and [schema_id] = SCHEMA_ID('q2'))
BEGIN

	CREATE TABLE [q2].[GroupableValue]
	(
		[GroupableValueId] INT NOT NULL IDENTITY(1,1),
		[GroupableLevelId] INT NOT NULL,
		[ValueLabel] NVARCHAR(100) NOT NULL,
		[DecimalValue] decimal(28,10),
        CONSTRAINT FK_GroupableValue_GroupableLevelId FOREIGN KEY ([GroupableLevelId]) REFERENCES [q2].[GroupableLevel] ([GroupableLevelId]),
        CONSTRAINT PK_GroupableValue PRIMARY KEY ([GroupableValueId])
	);

END

GO
 
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_GroupableValue_LevelValue')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_GroupableValue_LevelValue ON q2.GroupableValue (GroupableLevelId, ValueLabel, DecimalValue);
END
 
GO

CREATE OR ALTER PROCEDURE [q2].[InsertGroupableLevel]
    @NewGroupLabel NVARCHAR(100),
    @ParentGroupLabel NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ParentId INT = (SELECT GroupableLevelId FROM [q2].[GroupableLevel] WHERE GroupLabel = @ParentGroupLabel)

    INSERT INTO [q2].[GroupableLevel] (ParentId, GroupLabel)
    VALUES (@ParentId, @NewGroupLabel)
END

GO

CREATE OR ALTER PROCEDURE [q2].[InsertGroupableValue]
    @GroupLabel NVARCHAR(100),
	@ValueLabel NVARCHAR(100),
    @DecimalValue decimal(28,10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GroupableLevelId INT = (SELECT GroupableLevelId FROM [q2].[GroupableLevel] WHERE GroupLabel = @GroupLabel)

    INSERT INTO [q2].[GroupableValue] ([GroupableLevelId], [ValueLabel], [DecimalValue])
    VALUES (@GroupableLevelId, @ValueLabel, @DecimalValue)
END

GO

CREATE OR ALTER PROCEDURE [q2].[GetAllGroupValues]
AS
BEGIN
	select
		 l.GroupableLevelId
		,v.GroupableValueId
		,[GroupableLevelParentId] = l.ParentId
		,l.GroupLabel
		,v.ValueLabel
		,v.DecimalValue
		,[HeirarchyLevel] = l.GroupableLevelPath.GetLevel()
	from q2.GroupableLevel l
	left join q2.GroupableValue v on v.GroupableLevelId = l.GroupableLevelId
END

GO

--testing data
exec [q2].[InsertGroupableLevel] 'Group1', null
exec [q2].[InsertGroupableLevel] 'Group2', null
exec [q2].[InsertGroupableLevel] 'Group3', null
exec [q2].[InsertGroupableLevel] 'Sub 1', 'Group1'
exec [q2].[InsertGroupableLevel] 'Sub 2', 'Group1'
exec [q2].[InsertGroupableLevel] 'Sub 3', 'Group1'
exec [q2].[InsertGroupableLevel] 'Sub 4', 'Group1'
exec [q2].[InsertGroupableLevel] 'Sub 5', 'Group2'
exec [q2].[InsertGroupableLevel] 'Sub 6', 'Group2'
exec [q2].[InsertGroupableLevel] 'Sub 7', 'Group2'
exec [q2].[InsertGroupableLevel] 'Sub 8', 'Group3'
exec [q2].[InsertGroupableLevel] 'Sub 9', 'Group3'
exec [q2].[InsertGroupableLevel] 'Acct #1', 'Sub 1'
exec [q2].[InsertGroupableLevel] 'Acct #2', 'Sub 1'
exec [q2].[InsertGroupableLevel] 'Acct #3', 'Sub 1'
exec [q2].[InsertGroupableLevel] 'Acct #4', 'Sub 2'
exec [q2].[InsertGroupableLevel] 'Acct #5', 'Sub 3'
exec [q2].[InsertGroupableLevel] 'Acct #6', 'Sub 4'
exec [q2].[InsertGroupableLevel] 'Acct #7', 'Sub 4'
exec [q2].[InsertGroupableLevel] 'Acct #8', 'Sub 5'
exec [q2].[InsertGroupableLevel] 'Acct #9', 'Sub 5'
exec [q2].[InsertGroupableLevel] 'Acct #10', 'Sub 6'
exec [q2].[InsertGroupableLevel] 'Acct #11', 'Sub 7'
exec [q2].[InsertGroupableLevel] 'Acct #12', 'Sub 7'
exec [q2].[InsertGroupableLevel] 'Acct #13', 'Sub 8'
exec [q2].[InsertGroupableLevel] 'Acct #14', 'Sub 9'
--test for null handling
exec [q2].[InsertGroupableLevel] null, 'Sub 9'

exec [q2].[InsertGroupableValue] 'Acct #1', 'NAV', 1000000
exec [q2].[InsertGroupableValue] 'Acct #2', 'NAV', 2000000
exec [q2].[InsertGroupableValue] 'Acct #3', 'NAV', 150000
exec [q2].[InsertGroupableValue] 'Acct #4', 'NAV', 230000
exec [q2].[InsertGroupableValue] 'Acct #5', 'NAV', 1356000
exec [q2].[InsertGroupableValue] 'Acct #6', 'NAV', 780000
exec [q2].[InsertGroupableValue] 'Acct #7', 'NAV', 1900000
exec [q2].[InsertGroupableValue] 'Acct #8', 'NAV', 1000000
exec [q2].[InsertGroupableValue] 'Acct #9', 'NAV', 2000000
exec [q2].[InsertGroupableValue] 'Acct #10', 'NAV', 150000
exec [q2].[InsertGroupableValue] 'Acct #11', 'NAV', 230000
exec [q2].[InsertGroupableValue] 'Acct #12', 'NAV', 1356000
exec [q2].[InsertGroupableValue] 'Acct #13', 'NAV', 780000
exec [q2].[InsertGroupableValue] 'Acct #14', 'NAV', 1900000

exec [q2].[InsertGroupableValue] 'Acct #1', 'Qty', 50
exec [q2].[InsertGroupableValue] 'Acct #2', 'Qty', 30
exec [q2].[InsertGroupableValue] 'Acct #3', 'Qty', 10
exec [q2].[InsertGroupableValue] 'Acct #4', 'Qty', 23
exec [q2].[InsertGroupableValue] 'Acct #5', 'Qty', 16
exec [q2].[InsertGroupableValue] 'Acct #6', 'Qty', 11
exec [q2].[InsertGroupableValue] 'Acct #7', 'Qty', 8
exec [q2].[InsertGroupableValue] 'Acct #8', 'Qty', 50
exec [q2].[InsertGroupableValue] 'Acct #9', 'Qty', 30
exec [q2].[InsertGroupableValue] 'Acct #10', 'Qty', 10
exec [q2].[InsertGroupableValue] 'Acct #11', 'Qty', 23
exec [q2].[InsertGroupableValue] 'Acct #12', 'Qty', 16
exec [q2].[InsertGroupableValue] 'Acct #13', 'Qty', 11
exec [q2].[InsertGroupableValue] 'Acct #14', 'Qty', 8


drop table if exists #pivotValues;

select *
into #pivotValues
from
(
	SELECT *
	FROM
	(
		SELECT l.GroupableLevelId, l.GroupableLevelPath, ValueLabel, DecimalValue = ISNULL(DecimalValue, 0)
		FROM [q2].[GroupableValue] v
		JOIN [q2].[GroupableLevel] l on l.GroupableLevelId = v.GroupableLevelId
	) AS SourceData
	PIVOT
	(
		SUM(DecimalValue)
		FOR ValueLabel IN ([NAV], [Qty])
	) AS pv
) v

--we can now very quickly query aggregate in the heierarchy 
SELECT l.GroupableLevelId, GroupLabel, NAV = SUM(NAV), Qty = SUM(Qty)
from
(
	SELECT * 
	FROM [q2].[GroupableLevel] gv
	WHERE gv.GroupableLevelPath.IsDescendantOf(
		(SELECT GroupableLevelPath FROM [q2].[GroupableLevel] WHERE GroupLabel = 'Group3')
	) = 1
) l
join #pivotValues v ON v.GroupableLevelPath.IsDescendantOf(l.GroupableLevelPath) = 1
group by l.GroupableLevelId, l.GroupLabel

GO

DROP PROCEDURE IF EXISTS [q2].[GetGroupValues]
GO

DROP TYPE IF EXISTS dbo.IntIdTableType;
GO

CREATE TYPE dbo.IntIdTableType AS TABLE
(
	IntId INT NOT NULL
);

GO

DROP TYPE IF EXISTS dbo.StringTableType;
GO

CREATE TYPE dbo.StringTableType AS TABLE
(
	String varchar(500) NOT NULL
);

GO

CREATE OR ALTER PROCEDURE [q2].[GetGroupValues]
(
	@GroupableLevelId [dbo].[IntIdTableType] READONLY,
	@RequestedValues [dbo].[StringTableType] READONLY
)
AS
BEGIN
	select
		 l.GroupableLevelId
		,v.GroupableValueId
		,[GroupableLevelParentId] = l.ParentId
		,l.GroupLabel
		,v.ValueLabel
		,v.DecimalValue
		,[HeirarchyLevel] = l.GroupableLevelPath.GetLevel()
	from q2.GroupableLevel l
	left join q2.GroupableValue v on v.GroupableLevelId = l.GroupableLevelId
	JOIN @GroupableLevelId g on g.IntId = l.GroupableLevelId
	JOIN @RequestedValues rv on rv.String = ISNULL(v.ValueLabel, rv.String)
END

GO

--more tests
--exec [q2].[InsertGroupableLevel] 'Sub 10', 'Group3'
--exec [q2].[InsertGroupableLevel] 'Acct #16', 'Sub 10'
--exec [q2].[InsertGroupableLevel] 'SubAcct #1', 'Acct #16'
--exec [q2].[InsertGroupableLevel] 'SubAcct #2', 'Acct #16'
--exec [q2].[InsertGroupableValue] 'SubAcct #1', 'Beta', 22.5
--exec [q2].[InsertGroupableValue] 'SubAcct #2', 'Beta', 37.5
----exec [q2].[InsertGroupableValue] 'Acct #16', 'Qty', 1