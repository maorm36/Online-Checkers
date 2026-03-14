using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class GroupByCountryModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public GroupByCountryModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<CountryGroup> CountryGroups { get; set; } = new List<CountryGroup>();

        public async Task OnGetAsync()
        {
            // Query #29: Group players by country
            // Using LINQ, no loops for generating result
            // Use GameParticipants to include multi-player games
            CountryGroups = await _context.Countries
                .Include(c => c.Players)
                    .ThenInclude(p => p.GameParticipants)
                        .ThenInclude(gp => gp.Game)
                .Where(c => c.Players.Any())
                .Select(c => new CountryGroup
                {
                    CountryName = c.Name,
                    Players = c.Players.Select(p => new PlayerViewModel
                    {
                        IdentificationNumber = p.IdentificationNumber,
                        FirstName = p.FirstName,
                        Phone = p.Phone,
                        CountryName = c.Name,
                        GamesCount = p.GameParticipants
                            .Count(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                    }).ToList()
                })
                .OrderBy(c => c.CountryName)
                .ToListAsync();
        }
    }

    public class CountryGroup
    {
        public string CountryName { get; set; } = "";
        public List<PlayerViewModel> Players { get; set; } = new List<PlayerViewModel>();
    }
}
