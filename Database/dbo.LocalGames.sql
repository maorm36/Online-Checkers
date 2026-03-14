CREATE TABLE [dbo].[LocalGames] (
    [LocalGameId]      INT            IDENTITY (1, 1) NOT NULL,
    [ServerGameId]     INT            NULL,
    [PlayerName]       NVARCHAR (100) NOT NULL,
    [StartTime]        DATETIME       NOT NULL,
    [EndTime]          DATETIME       NULL,
    [Result]           NVARCHAR (50)  NULL,
    [TimeLimitSeconds] INT            NOT NULL,
    [Difficulty]       INT            DEFAULT ((2)) NOT NULL,
    [AllPlayerNames]   NVARCHAR (500) NULL,
    [TotalPlayers]     INT            DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([LocalGameId] ASC)
);

