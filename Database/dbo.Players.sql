CREATE TABLE [dbo].[Players] (
    [PlayerId]             INT           IDENTITY (1, 1) NOT NULL,
    [FirstName]            NVARCHAR (50) NOT NULL,
    [IdentificationNumber] INT           NOT NULL,
    [Phone]                NVARCHAR (10) NOT NULL,
    [CountryId]            INT           NOT NULL,
    CONSTRAINT [PK_Players] PRIMARY KEY CLUSTERED ([PlayerId] ASC),
    CONSTRAINT [FK_Players_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Countries] ([CountryId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Players_CountryId]
    ON [dbo].[Players]([CountryId] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Players_IdentificationNumber]
    ON [dbo].[Players]([IdentificationNumber] ASC);

