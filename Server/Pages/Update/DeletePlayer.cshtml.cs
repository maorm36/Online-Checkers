using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;
using CheckersServer.Pages.Queries;

namespace CheckersServer.Pages.Update
{
    public class DeletePlayerModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public DeletePlayerModel(CheckersDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? SelectedPlayerId { get; set; }

        public SelectList? PlayerList { get; set; }
        public List<PlayerViewModel> Players { get; set; } = new List<PlayerViewModel>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadData();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedPlayerId.HasValue)
            {
                var player = await _context.Players.FindAsync(SelectedPlayerId.Value);
                if (player != null)
                {
                    var playerName = player.FirstName;

                    // Safety: don't allow deleting a player who is currently part of a game that hasn't finished.
                    // (Deleting them would break an in-progress / registered same-screen game.)
                    var isInActiveGame = await _context.GameParticipants
                        .Where(gp => gp.PlayerId == player.PlayerId)
                        .AnyAsync(gp => gp.Game != null && (gp.Game.Result == "InProgress" || gp.Game.Result == "Registered"));

                    if (isInActiveGame)
                    {
                        ErrorMessage = $"לא ניתן למחוק את השחקן '{playerName}' כי הוא משויך למשחק פעיל/רשום. מחק קודם את המשחק או סיים אותו.";
                        await LoadData();
                        return Page();
                    }

                    // IMPORTANT: players can now participate in other players' games via GameParticipants.
                    // Because the FK to Players is NO ACTION (to avoid multiple cascade paths), we must
                    // remove the join rows explicitly before deleting the player.
                    var participantRows = await _context.GameParticipants
                        .Where(gp => gp.PlayerId == player.PlayerId)
                        .ToListAsync();

                    if (participantRows.Count > 0)
                    {
                        _context.GameParticipants.RemoveRange(participantRows);
                    }

                    // Delete player (cascade delete will still delete all games where he is the "owner" PlayerId)
                    _context.Players.Remove(player);
                    await _context.SaveChangesAsync();

                    SuccessMessage = $"השחקן '{playerName}' וכל המשחקים שלו נמחקו בהצלחה!";
                }
            }

            await LoadData();
            return Page();
        }

        private async Task LoadData()
        {
            // Load player list using LINQ
            var playerItems = await _context.Players
                .OrderBy(p => p.FirstName)
                .Select(p => new { p.PlayerId, Display = $"{p.FirstName} (מזהה: {p.IdentificationNumber})" })
                .ToListAsync();

            PlayerList = new SelectList(playerItems, "PlayerId", "Display");

            // Load players for display
            Players = await _context.Players
                .Include(p => p.Country)
                .Include(p => p.Games)
                .Select(p => new PlayerViewModel
                {
                    IdentificationNumber = p.IdentificationNumber,
                    FirstName = p.FirstName,
                    Phone = p.Phone,
                    CountryName = p.Country != null ? p.Country.Name : "",
                    GamesCount = p.Games.Count
                })
                .OrderBy(p => p.FirstName)
                .ToListAsync();
        }
    }
}
