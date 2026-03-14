using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;
using CheckersServer.Pages.Queries;

namespace CheckersServer.Pages.Update
{
    public class DeleteGameModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public DeleteGameModel(CheckersDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? SelectedGameId { get; set; }

        public SelectList? GameList { get; set; }
        public List<DeleteGameViewModel> Games { get; set; } = new List<DeleteGameViewModel>();
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadData();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedGameId.HasValue)
            {
                var game = await _context.Games.FindAsync(SelectedGameId.Value);
                if (game != null)
                {
                    // Delete game (cascade delete will delete all moves due to FK relationship)
                    _context.Games.Remove(game);
                    await _context.SaveChangesAsync();

                    SuccessMessage = $"משחק מספר {SelectedGameId} נמחק בהצלחה!";
                }
            }

            await LoadData();
            return Page();
        }

        private async Task LoadData()
        {
            // Load all games with their participants
            var gamesWithParticipants = await _context.Games
                .Include(g => g.Participants)
                    .ThenInclude(p => p.Player)
                .OrderByDescending(g => g.StartTime)
                .ToListAsync();

            // Build game list for dropdown - show all player names
            var gameItems = gamesWithParticipants
                .Select(g => new {
                    g.GameId,
                    Display = $"משחק {g.GameId} - {GetAllPlayerNames(g)} ({g.StartTime:dd/MM/yyyy})"
                })
                .ToList();

            GameList = new SelectList(gameItems, "GameId", "Display");

            // Load games for display table - show all player names
            Games = gamesWithParticipants
                .Select(g => new DeleteGameViewModel
                {
                    GameId = g.GameId,
                    PlayerNames = GetAllPlayerNames(g),
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    Duration = g.Duration,
                    Result = g.Result,
                    TimeLimitSeconds = g.TimeLimitSeconds
                })
                .ToList();
        }

        /// <summary>
        /// Get all player names for a game, comma-separated
        /// </summary>
        private string GetAllPlayerNames(CheckersServer.Models.GameSession game)
        {
            if (game.Participants != null && game.Participants.Any())
            {
                var names = game.Participants
                    .OrderBy(p => p.TurnOrder)
                    .Select(p => p.Player?.FirstName ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (names.Any())
                    return string.Join(", ", names);
            }

            // Fallback to primary player
            return game.Player?.FirstName ?? "לא ידוע";
        }
    }

    public class DeleteGameViewModel
    {
        public int GameId { get; set; }
        public string PlayerNames { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Result { get; set; }
        public int TimeLimitSeconds { get; set; }
    }
}