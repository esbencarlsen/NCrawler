CREATE TABLE [dbo].[CrawlHistory] (
    [Id]      INT             IDENTITY (1, 1) NOT NULL,
    [Key]     NVARCHAR (1024) NOT NULL,
    [GroupId] INT             NOT NULL
);

