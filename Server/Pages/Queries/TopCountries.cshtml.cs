using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class TopCountriesModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public TopCountriesModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<CountryGamesCount> TopCountries { get; set; } = new List<CountryGamesCount>();

        public async Task OnGetAsync()
        {
            // Query #30: Top 2 countries by most games played
            // Show only two columns: country name and games count
            // Using LINQ, no loops for generating result
            // Use GameParticipants to include multi-player games
            TopCountries = await _context.Countries
                .Include(c => c.Players)
                    .ThenInclude(p => p.GameParticipants)
                        .ThenInclude(gp => gp.Game)
                .Select(c => new CountryGamesCount
                {
                    CountryName = c.Name,
                    GamesCount = c.Players
                        .SelectMany(p => p.GameParticipants)
                        .Count(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                })
                .Where(c => c.GamesCount > 0)
                .OrderByDescending(c => c.GamesCount)
                .Take(2)
                .ToListAsync();
        }
    }

    public class CountryGamesCount
    {
        public string CountryName { get; set; } = "";
        public int GamesCount { get; set; }
    }
}
