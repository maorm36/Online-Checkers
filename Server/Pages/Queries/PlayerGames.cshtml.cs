using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class PlayerGamesModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public PlayerGamesModel(CheckersDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string? SelectedPlayerName { get; set; }

        public SelectList? PlayerNames { get; set; }

        public List<GameViewModel> Games { get; set; } = new List<GameViewModel>();

        public async Task OnGetAsync()
        {
            await LoadPlayerNames();
        }

        public async Task OnPostAsync()
        {
            await LoadPlayerNames();

            if (!string.IsNullOrEmpty(SelectedPlayerName))
            {
                // Query #26: Get all games for selected player name
                // Player names in combo are unique (case-insensitive), ascending order
                // Using LINQ, no loops for generating result
                // Use GameParticipants to include multi-player games
                Games = await _context.GameParticipants
                    .Include(gp => gp.Game)
                    .Include(gp => gp.Player)
                    .Where(gp => gp.Player != null && 
                                gp.Player.FirstName.ToLower() == SelectedPlayerName.ToLower() &&
                                gp.Game != null)
                    .Select(gp => new GameViewModel
                    {
                        GameId = gp.GameId,
                        PlayerName = gp.Player != null ? gp.Player.FirstName : "",
                        StartTime = gp.Game!.StartTime,
                        EndTime = gp.Game.EndTime,
                        Duration = gp.Game.Duration,
                        Result = gp.Game.Result,
                        TimeLimitSeconds = gp.Game.TimeLimitSeconds
                    })
                    .OrderByDescending(g => g.StartTime)
                    .ToListAsync();
            }
        }

        private async Task LoadPlayerNames()
        {
            // Query #26: Unique names without duplicates (case-insensitive), ascending order
            // "Avi" and "avI" are considered duplicates
            var names = await _context.Players
                .Select(p => p.FirstName)
                .ToListAsync();

            // Group by lowercase to remove case-insensitive duplicates, then take first occurrence
            var uniqueNames = names
                .GroupBy(n => n.ToLower())
                .Select(g => g.First())
                .OrderBy(n => n.ToLower())
                .ToList();

            PlayerNames = new SelectList(uniqueNames);
        }
    }
}
