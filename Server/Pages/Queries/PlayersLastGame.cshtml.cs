using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class PlayersLastGameModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public PlayersLastGameModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<PlayerLastGameViewModel> Players { get; set; } = new List<PlayerLastGameViewModel>();

        public async Task OnGetAsync()
        {
            // Query #23: All players sorted descending by name (case insensitive)
            // Only show name and last game date (two columns only)
            // Only players with at least one completed game
            // Use GameParticipants to include multi-player games
            Players = await _context.Players
                .Include(p => p.GameParticipants)
                    .ThenInclude(gp => gp.Game)
                .Where(p => p.GameParticipants.Any(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered"))
                .Select(p => new PlayerLastGameViewModel
                {
                    FirstName = p.FirstName,
                    LastGameDate = p.GameParticipants
                        .Where(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                        .Max(gp => gp.Game!.StartTime)
                })
                .OrderByDescending(p => p.FirstName.ToLower())
                .ToListAsync();
        }
    }

    public class PlayerLastGameViewModel
    {
        public string FirstName { get; set; } = "";
        public DateTime LastGameDate { get; set; }
    }
}
