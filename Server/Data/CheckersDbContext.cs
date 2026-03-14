using Microsoft.EntityFrameworkCore;
using CheckersServer.Models;

namespace CheckersServer.Data
{
    public class CheckersDbContext : DbContext
    {
        public CheckersDbContext(DbContextOptions<CheckersDbContext> options) : base(options)
        {
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<GameSession> Games { get; set; }
        public DbSet<GameMove> Moves { get; set; }
        public DbSet<SoldierBackwardUsed> SoldierBackwardsUsed { get; set; }
        public DbSet<GameParticipant> GameParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraint for IdentificationNumber
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.IdentificationNumber)
                .IsUnique();

            // Configure cascade delete relationships
            modelBuilder.Entity<Player>()
                .HasMany(p => p.Games)
                .WithOne(g => g.Player)
                .HasForeignKey(g => g.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GameSession>()
                .HasMany(g => g.Moves)
                .WithOne(m => m.Game)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure game participants (many-to-many via join entity)
            modelBuilder.Entity<GameParticipant>()
                .HasKey(gp => new { gp.GameId, gp.PlayerId });

            modelBuilder.Entity<GameParticipant>()
                .HasOne(gp => gp.Game)
                .WithMany(g => g.Participants)
                .HasForeignKey(gp => gp.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GameParticipant>()
                .HasOne(gp => gp.Player)
                .WithMany(p => p.GameParticipants)
                .HasForeignKey(gp => gp.PlayerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GameParticipant>()
                .HasIndex(gp => gp.PlayerId);

            // Seed initial countries data
            modelBuilder.Entity<Country>().HasData(
                new Country { CountryId = 1, Name = "ישראל" },
                new Country { CountryId = 2, Name = "ארצות הברית" },
                new Country { CountryId = 3, Name = "בריטניה" },
                new Country { CountryId = 4, Name = "צרפת" },
                new Country { CountryId = 5, Name = "גרמניה" },
                new Country { CountryId = 6, Name = "איטליה" },
                new Country { CountryId = 7, Name = "ספרד" },
                new Country { CountryId = 8, Name = "קנדה" },
                new Country { CountryId = 9, Name = "אוסטרליה" },
                new Country { CountryId = 10, Name = "יפן" }
            );

            // Seed sample players data
            modelBuilder.Entity<Player>().HasData(
                new Player { PlayerId = 1, FirstName = "דוד", IdentificationNumber = 100, Phone = "0501234567", CountryId = 1 },
                new Player { PlayerId = 2, FirstName = "שרה", IdentificationNumber = 101, Phone = "0521234567", CountryId = 1 },
                new Player { PlayerId = 3, FirstName = "John", IdentificationNumber = 102, Phone = "0541234567", CountryId = 2 },
                new Player { PlayerId = 4, FirstName = "Marie", IdentificationNumber = 103, Phone = "0551234567", CountryId = 4 },
                new Player { PlayerId = 5, FirstName = "Hans", IdentificationNumber = 104, Phone = "0561234567", CountryId = 5 },
                new Player { PlayerId = 6, FirstName = "יוסי", IdentificationNumber = 105, Phone = "0571234567", CountryId = 1 },
                new Player { PlayerId = 7, FirstName = "Emma", IdentificationNumber = 106, Phone = "0581234567", CountryId = 3 },
                new Player { PlayerId = 8, FirstName = "Luigi", IdentificationNumber = 107, Phone = "0591234567", CountryId = 6 }
            );

            // Seed sample games data
            modelBuilder.Entity<GameSession>().HasData(
                new GameSession { GameId = 1, PlayerId = 1, StartTime = DateTime.Now.AddDays(-10), EndTime = DateTime.Now.AddDays(-10).AddMinutes(15), Duration = TimeSpan.FromMinutes(15), Result = "Win", TimeLimitSeconds = 10 },
                new GameSession { GameId = 2, PlayerId = 1, StartTime = DateTime.Now.AddDays(-8), EndTime = DateTime.Now.AddDays(-8).AddMinutes(20), Duration = TimeSpan.FromMinutes(20), Result = "Loss", TimeLimitSeconds = 10 },
                new GameSession { GameId = 3, PlayerId = 2, StartTime = DateTime.Now.AddDays(-7), EndTime = DateTime.Now.AddDays(-7).AddMinutes(12), Duration = TimeSpan.FromMinutes(12), Result = "Win", TimeLimitSeconds = 5 },
                new GameSession { GameId = 4, PlayerId = 3, StartTime = DateTime.Now.AddDays(-5), EndTime = DateTime.Now.AddDays(-5).AddMinutes(18), Duration = TimeSpan.FromMinutes(18), Result = "Loss", TimeLimitSeconds = 15 },
                new GameSession { GameId = 5, PlayerId = 4, StartTime = DateTime.Now.AddDays(-3), EndTime = DateTime.Now.AddDays(-3).AddMinutes(25), Duration = TimeSpan.FromMinutes(25), Result = "Win", TimeLimitSeconds = 10 },
                new GameSession { GameId = 6, PlayerId = 1, StartTime = DateTime.Now.AddDays(-2), EndTime = DateTime.Now.AddDays(-2).AddMinutes(22), Duration = TimeSpan.FromMinutes(22), Result = "Win", TimeLimitSeconds = 10 },
                new GameSession { GameId = 7, PlayerId = 5, StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now.AddDays(-1).AddMinutes(16), Duration = TimeSpan.FromMinutes(16), Result = "Loss", TimeLimitSeconds = 2 }
            );

            // Seed GameParticipants for each game (link each game to its player)
            modelBuilder.Entity<GameParticipant>().HasData(
                new GameParticipant { GameId = 1, PlayerId = 1, TurnOrder = 1 },
                new GameParticipant { GameId = 2, PlayerId = 1, TurnOrder = 1 },
                new GameParticipant { GameId = 3, PlayerId = 2, TurnOrder = 1 },
                new GameParticipant { GameId = 4, PlayerId = 3, TurnOrder = 1 },
                new GameParticipant { GameId = 5, PlayerId = 4, TurnOrder = 1 },
                new GameParticipant { GameId = 6, PlayerId = 1, TurnOrder = 1 },
                new GameParticipant { GameId = 7, PlayerId = 5, TurnOrder = 1 }
            );
        }
    }
}
