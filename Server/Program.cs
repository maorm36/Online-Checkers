using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<CheckersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Create database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CheckersDbContext>();
    context.Database.EnsureCreated();

    // If the DB already exists (EnsureCreated does nothing), we still need to add new tables
    // required for the "same game" feature (multiple participants on the same client).
    // This is SAFE: it only creates the table if it's missing.
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[GameParticipants]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GameParticipants](
        [GameId] INT NOT NULL,
        [PlayerId] INT NOT NULL,
        [TurnOrder] INT NOT NULL,
        CONSTRAINT [PK_GameParticipants] PRIMARY KEY ([GameId], [PlayerId]),
        CONSTRAINT [FK_GameParticipants_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games]([GameId]) ON DELETE CASCADE,
        CONSTRAINT [FK_GameParticipants_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_GameParticipants_PlayerId] ON [dbo].[GameParticipants]([PlayerId]);
END
");

}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();