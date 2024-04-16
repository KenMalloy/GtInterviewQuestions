use [GtInterview]
go
/*
Question 1
Security (ID, Name,Ticker) can have Multiple types of Ratings for Rating Type of Credit, Liquidity, Market e.t.c.
Multiple agencies can quote ratings like S&P, Moodys,Fitch, Internal e.t.c.
We can receive Ratings daily and updates can also be received.
Create all the needed tables with Keys e.t.c to store these.
Goal is to be able to pull one latest row for each Rating Type for a given ticker.
Name, Ticker, Ratig Type, Agency, Rating, Date Received
*/

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'q1')
BEGIN
    EXEC('CREATE SCHEMA q1');
END
GO

DROP TABLE IF EXISTS q1.SecurityRating;
DROP TABLE IF EXISTS q1.QuoteSource;
DROP TABLE IF EXISTS q1.RatingType;
DROP TABLE IF EXISTS q1.SecurityInfo;
 
DROP TABLE IF EXISTS #generatedDates;
DROP TABLE IF EXISTS #allEntities;
 
SET NOCOUNT ON;
 
GO
 
CREATE TABLE q1.SecurityInfo
(
       SecurityInfoId int not null identity(1,1),
       SecurityName nvarchar(100) not null,
       Ticker nvarchar(25) not null,
       CONSTRAINT PK_SecurityInfo PRIMARY KEY (SecurityInfoId)
)
 
GO
 
INSERT INTO [q1].[SecurityInfo] (Ticker, SecurityName)
VALUES
       ('AMD','Advanced Micro Devices, Inc.'),
       ('TSLA','Tesla, Inc.'),
       ('INTC','Intel Corporation'),
       ('RIVN','Rivian Automotive, Inc.')
  
GO
 
CREATE TABLE q1.RatingType
(
       RatingTypeId int not null identity(1,1),
       RatingTypeCode varchar(25) not null,
       CONSTRAINT PK_RatingType PRIMARY KEY (RatingTypeId)
)
GO
 
INSERT INTO [q1].[RatingType]
VALUES
       ('Credit'), ('Liquidity'), ('Market')
 
GO
 
CREATE TABLE q1.QuoteSource
(
       QuoteSourceId int not null identity(1,1),
       QuoteSourceCode varchar(25) not null,
       CONSTRAINT PK_QuoteSource PRIMARY KEY (QuoteSourceId)
)
GO
 
INSERT INTO [q1].[QuoteSource] (QuoteSourceCode)
VALUES
       ('S&P'), ('Moodys'), ('Fitch'), ('Internal')
 
GO
 
CREATE TABLE q1.SecurityRating
(
    SecurityRatingId int not null identity(1,1),
    SecurityInfoId int not null,
              CONSTRAINT FK_SecurityRating_SecurityInfo FOREIGN KEY (SecurityInfoId) REFERENCES q1.SecurityInfo (SecurityInfoId),
       RatingTypeId int not null,
              CONSTRAINT FK_SecurityRating_RatingType FOREIGN KEY (RatingTypeId) REFERENCES q1.RatingType (RatingTypeId),
       QuoteSourceId int not null,
              CONSTRAINT FK_SecurityRating_QuoteSource FOREIGN KEY (QuoteSourceId) REFERENCES q1.QuoteSource (QuoteSourceId),
       Rating varchar(10) not null,
       DateReceived datetime not null,
       CONSTRAINT PK_SecurityRating PRIMARY KEY (SecurityInfoId, RatingTypeId, QuoteSourceId, DateReceived DESC),
       CONSTRAINT UX_SecurityRating_DateReceived UNIQUE NONCLUSTERED (DateReceived DESC)
);
 
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SecurityRating_Rating')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SecurityRating_Rating ON q1.SecurityRating (DateReceived);
END
 
GO
 
--BEGIN DATA POPULATION
BEGIN
 
declare @startDate datetime = '04/01/2024'
declare @endDate datetime = '04/02/2024'
 
;with cte as (
    select
              [Date] = @startDate
    union all
    select
              [Date] = DATEADD(MINUTE, 1, [Date])
    from cte
    where [Date] < @endDate
)
SELECT
       [Date]
INTO #generatedDates
FROM cte dates
option(maxrecursion 0);
--select * from #generatedDates order by 1
 
select *
into #allEntities
from q1.[SecurityInfo]
FULL OUTER JOIN q1.[RatingType] ON 1 = 1
FULL OUTER JOIN q1.[QuoteSource] ON 1 = 1
 
ALTER TABLE #generatedDates ADD [GeneratedDateId] INT IDENTITY (1,1)
DECLARE @allEntitiesCount int = (select count(*) from #allEntities)
 
SET NOCOUNT OFF;
 
INSERT INTO q1.SecurityRating (SecurityInfoId,RatingTypeId,QuoteSourceId,Rating,DateReceived)
SELECT ae.SecurityInfoId, ae.RatingTypeId, ae.QuoteSourceId,
       [Rating] =
              CASE
                     WHEN (ae.SecurityInfoId + ae.RatingTypeId + ae.QuoteSourceId) % 4 = 0 then 'AAA'
                     WHEN (ae.SecurityInfoId + ae.RatingTypeId + ae.QuoteSourceId) % 4 = 1 then 'AA'
                     WHEN (ae.SecurityInfoId + ae.RatingTypeId + ae.QuoteSourceId) % 4 = 2 then 'B+'
                     WHEN (ae.SecurityInfoId + ae.RatingTypeId + ae.QuoteSourceId) % 4 = 3 then 'C'
              END
,[gd].[Date]
from #generatedDates gd
left JOIN --randomly matches a <[SecurityInfo]/[RatingType]/[QuoteSource]> triple with one of the generated event times
(
       SELECT
              rowNum = ROW_NUMBER() OVER(ORDER BY NEWID())
              ,*
              FROM
       #allEntities
) ae
ON ae.rowNum = ((gd.[GeneratedDateId] % @allEntitiesCount)+1)
 
END
 
GO
 
--Goal is to be able to pull one latest row for each Rating Type for a given ticker.
--Name, Ticker, Ratig Type, Agency, Rating, Date Received
DECLARE @Ticker nvarchar(25) = 'INTC'
 
select
       si.SecurityName
       ,si.Ticker
       ,rt.RatingTypeCode
       ,[Agency] = qs.QuoteSourceCode
       ,[Rating] = sr.Rating
       ,sr.DateReceived
from q1.RatingType rt
JOIN q1.SecurityRating sr ON sr.RatingTypeId = rt.RatingTypeId
JOIN q1.SecurityInfo si ON si.SecurityInfoId = sr.SecurityInfoId
JOIN q1.QuoteSource qs ON qs.QuoteSourceId = sr.QuoteSourceId
JOIN
(
       select SecurityInfoId,RatingTypeId,DateReceived = max(DateReceived)
       from q1.SecurityRating
       group by SecurityInfoId,RatingTypeId
) latest ON latest.SecurityInfoId = sr.SecurityInfoId AND latest.RatingTypeId = sr.RatingTypeId AND latest.DateReceived = sr.DateReceived
WHERE si.Ticker = @Ticker
 
/* --another test
select * from #allEntities
--markit, INTC, S&P
insert into SecurityRating (RatingTypeId, SecurityInfoId, QuoteSourceId, Rating, DateReceived)
values (3,3, 1, 'BB', getdate())
*/
 
--latest for all
select
       si.SecurityName
       ,si.Ticker
       ,rt.RatingTypeCode
       ,[Agency] = qs.QuoteSourceCode
       ,[Rating] = sr.Rating
       ,sr.DateReceived
from q1.RatingType rt
JOIN q1.SecurityRating sr ON sr.RatingTypeId = rt.RatingTypeId
JOIN q1.SecurityInfo si ON si.SecurityInfoId = sr.SecurityInfoId
JOIN q1.QuoteSource qs ON qs.QuoteSourceId = sr.QuoteSourceId
JOIN
(
       select SecurityInfoId,RatingTypeId,DateReceived = max(DateReceived)
       from q1.SecurityRating
       group by SecurityInfoId,RatingTypeId
) latest ON latest.SecurityInfoId = sr.SecurityInfoId AND latest.RatingTypeId = sr.RatingTypeId AND latest.DateReceived = sr.DateReceived
ORDER BY si.Ticker, rt.RatingTypeCode

GO
