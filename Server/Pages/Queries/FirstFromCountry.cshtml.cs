using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class FirstFromCountryModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public FirstFromCountryModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<FirstPlayerViewModel> Players { get; set; } = new List<FirstPlayerViewModel>();

        public async Task OnGetAsync()
        {
            // Query #25: First player from each country who ever played
            // Using LINQ with client-side evaluation for complex grouping
            // Use GameParticipants to include multi-player games

            // Step 1: Fetch all relevant data from database
            var playersWithGames = await _context.Players
                .Include(p => p.Country)
                .Include(p => p.GameParticipants)
                    .ThenInclude(gp => gp.Game)
                .ToListAsync();

            // Step 2: Filter and process in memory
            Players = playersWithGames
                .Where(p => p.GameParticipants.Any(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered"))
                .GroupBy(p => p.CountryId)
                .Select(g =>
                {
                    // Find the player with the earliest game in this country
                    var firstPlayer = g
                        .Select(p => new
                        {
                            Player = p,
                            FirstGameDate = p.GameParticipants
                                .Where(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                                .Min(gp => gp.Game!.StartTime)
                        })
                        .OrderBy(x => x.FirstGameDate)
                        .First();

                    return new FirstPlayerViewModel
                    {
                        CountryName = firstPlayer.Player.Country?.Name ?? "",
                        FirstName = firstPlayer.Player.FirstName,
                        IdentificationNumber = firstPlayer.Player.IdentificationNumber,
                        FirstGameDate = firstPlayer.FirstGameDate
                    };
                })
                .OrderBy(p => p.CountryName)
                .ToList();
        }
    }

    public class FirstPlayerViewModel
    {
        public string CountryName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public int IdentificationNumber { get; set; }
        public DateTime FirstGameDate { get; set; }
    }
}