using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class GamesCountModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public GamesCountModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<PlayerGamesCountViewModel> PlayerCounts { get; set; } = new List<PlayerGamesCountViewModel>();

        public async Task OnGetAsync()
        {
            // Query #27: Each player, how many games they played
            // Show only two columns: name and count (no additional info)
            // Using LINQ, no loops for generating result
            // Count games via GameParticipants to include multi-player games
            PlayerCounts = await _context.Players
                .Include(p => p.GameParticipants)
                    .ThenInclude(gp => gp.Game)
                .Select(p => new PlayerGamesCountViewModel
                {
                    PlayerName = p.FirstName,
                    GamesCount = p.GameParticipants
                        .Count(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                })
                .OrderByDescending(p => p.GamesCount)
                .ThenBy(p => p.PlayerName)
                .ToListAsync();
        }
    }

    public class PlayerGamesCountViewModel
    {
        public string PlayerName { get; set; } = "";
        public int GamesCount { get; set; }
    }
}
