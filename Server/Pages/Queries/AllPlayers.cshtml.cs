using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class AllPlayersModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public AllPlayersModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<PlayerViewModel> Players { get; set; } = new List<PlayerViewModel>();

        public async Task OnGetAsync()
        {
            // Query #22: All players who played (at least one completed game) with all details
            // Sorted by name case-insensitive, using LINQ (no loops for generating the result)
            // Use GameParticipants to include multi-player games
            Players = await _context.Players
                .Include(p => p.Country)
                .Include(p => p.GameParticipants)
                    .ThenInclude(gp => gp.Game)
                .Where(p => p.GameParticipants.Any(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered"))
                .Select(p => new PlayerViewModel
                {
                    IdentificationNumber = p.IdentificationNumber,
                    FirstName = p.FirstName,
                    Phone = p.Phone,
                    CountryName = p.Country != null ? p.Country.Name : "",
                    GamesCount = p.GameParticipants
                        .Count(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                })
                .OrderBy(p => p.FirstName.ToLower())
                .ToListAsync();
        }
    }

    public class PlayerViewModel
    {
        public int IdentificationNumber { get; set; }
        public string FirstName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CountryName { get; set; } = "";
        public int GamesCount { get; set; }
    }
}
