CREATE TABLE [dbo].[Moves] (
    [MoveId]         INT           IDENTITY (1, 1) NOT NULL,
    [GameId]         INT           NOT NULL,
    [MoveNumber]     INT           NOT NULL,
    [IsPlayerMove]   BIT           NOT NULL,
    [FromRow]        INT           NOT NULL,
    [FromCol]        INT           NOT NULL,
    [ToRow]          INT           NOT NULL,
    [ToCol]          INT           NOT NULL,
    [IsCapture]      BIT           NOT NULL,
    [IsBackwardMove] BIT           NOT NULL,
    [MoveTime]       DATETIME2 (7) NOT NULL,
    CONSTRAINT [PK_Moves] PRIMARY KEY CLUSTERED ([MoveId] ASC),
    CONSTRAINT [FK_Moves_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games] ([GameId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Moves_GameId]
    ON [dbo].[Moves]([GameId] ASC);

