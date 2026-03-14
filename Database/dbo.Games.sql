CREATE TABLE [dbo].[Games] (
    [GameId]           INT           IDENTITY (1, 1) NOT NULL,
    [PlayerId]         INT           NOT NULL,
    [StartTime]        DATETIME2 (7) NOT NULL,
    [EndTime]          DATETIME2 (7) NULL,
    [Duration]         TIME (7)      NULL,
    [Result]           NVARCHAR (20) NULL,
    [TimeLimitSeconds] INT           NOT NULL,
    [Difficulty]       INT           DEFAULT ((2)) NOT NULL,
    CONSTRAINT [PK_Games] PRIMARY KEY CLUSTERED ([GameId] ASC),
    CONSTRAINT [FK_Games_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Games_PlayerId]
    ON [dbo].[Games]([PlayerId] ASC);

