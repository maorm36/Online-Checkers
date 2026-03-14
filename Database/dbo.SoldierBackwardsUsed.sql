CREATE TABLE [dbo].[SoldierBackwardsUsed] (
    [Id]       INT IDENTITY (1, 1) NOT NULL,
    [GameId]   INT NOT NULL,
    [Row]      INT NOT NULL,
    [Col]      INT NOT NULL,
    [IsPlayer] BIT NOT NULL,
    CONSTRAINT [PK_SoldierBackwardsUsed] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SoldierBackwardsUsed_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games] ([GameId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_SoldierBackwardsUsed_GameId]
    ON [dbo].[SoldierBackwardsUsed]([GameId] ASC);

