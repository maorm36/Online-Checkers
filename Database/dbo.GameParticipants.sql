CREATE TABLE [dbo].[GameParticipants] (
    [GameId]    INT NOT NULL,
    [PlayerId]  INT NOT NULL,
    [TurnOrder] INT NOT NULL,
    CONSTRAINT [PK_GameParticipants] PRIMARY KEY CLUSTERED ([GameId] ASC, [PlayerId] ASC),
    CONSTRAINT [FK_GameParticipants_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games] ([GameId]) ON DELETE CASCADE,
    CONSTRAINT [FK_GameParticipants_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId])
);


GO
CREATE NONCLUSTERED INDEX [IX_GameParticipants_PlayerId]
    ON [dbo].[GameParticipants]([PlayerId] ASC);

