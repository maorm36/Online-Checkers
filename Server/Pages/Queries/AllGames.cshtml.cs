using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class AllGamesModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public AllGamesModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<GameViewModel> Games { get; set; } = new List<GameViewModel>();

        public async Task OnGetAsync()
        {
            // Query #24: All games with all details (using LINQ, no loops for generating result)
            // Show a row for EACH participant in each game (via GameParticipants join table)
            Games = await _context.GameParticipants
                .Include(gp => gp.Game)
                .Include(gp => gp.Player)
                .Where(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                .Select(gp => new GameViewModel
                {
                    GameId = gp.GameId,
                    PlayerName = gp.Player != null ? gp.Player.FirstName : "",
                    StartTime = gp.Game!.StartTime,
                    EndTime = gp.Game.EndTime,
                    Duration = gp.Game.Duration,
                    Result = gp.Game.Result,
                    TimeLimitSeconds = gp.Game.TimeLimitSeconds,
                    Difficulty = gp.Game.Difficulty
                })
                .OrderByDescending(g => g.StartTime)
                .ThenBy(g => g.GameId)
                .ThenBy(g => g.PlayerName)
                .ToListAsync();
        }
    }

    public class GameViewModel
    {
        public int GameId { get; set; }
        public string PlayerName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Result { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int Difficulty { get; set; }
    }
}
