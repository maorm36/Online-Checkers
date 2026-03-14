CREATE TABLE [dbo].[LocalMoves] (
    [LocalMoveId]    INT      IDENTITY (1, 1) NOT NULL,
    [LocalGameId]    INT      NOT NULL,
    [MoveNumber]     INT      NOT NULL,
    [IsPlayerMove]   BIT      NOT NULL,
    [FromRow]        INT      NOT NULL,
    [FromCol]        INT      NOT NULL,
    [ToRow]          INT      NOT NULL,
    [ToCol]          INT      NOT NULL,
    [IsCapture]      BIT      NOT NULL,
    [IsBackwardMove] BIT      NOT NULL,
    [MoveTime]       DATETIME NOT NULL,
    PRIMARY KEY CLUSTERED ([LocalMoveId] ASC),
    FOREIGN KEY ([LocalGameId]) REFERENCES [dbo].[LocalGames] ([LocalGameId]) ON DELETE CASCADE
);

