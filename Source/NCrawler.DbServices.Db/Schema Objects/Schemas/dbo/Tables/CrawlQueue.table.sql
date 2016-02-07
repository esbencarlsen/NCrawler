CREATE TABLE [dbo].[CrawlQueue] (
    [Id]             INT             IDENTITY (1, 1) NOT NULL,
    [GroupId]        INT             NOT NULL,
    [SerializedData] VARBINARY (MAX) NULL
);

