using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages.Queries
{
    public class GroupByGamesCountModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public GroupByGamesCountModel(CheckersDbContext context)
        {
            _context = context;
        }

        public List<GamesCountGroup> Groups { get; set; } = new List<GamesCountGroup>();

        public async Task OnGetAsync()
        {
            // Query #28: Group players by games count, sorted descending by count
            // Using LINQ, no loops for generating result
            // Use GameParticipants to include multi-player games
            var playersWithCounts = await _context.Players
                .Include(p => p.Country)
                .Include(p => p.GameParticipants)
                    .ThenInclude(gp => gp.Game)
                .Select(p => new
                {
                    Player = p,
                    GamesCount = p.GameParticipants
                        .Count(gp => gp.Game != null && gp.Game.Result != "InProgress" && gp.Game.Result != "Registered")
                })
                .ToListAsync();

            Groups = playersWithCounts
                .GroupBy(p => p.GamesCount)
                .Select(g => new GamesCountGroup
                {
                    GamesCount = g.Key,
                    Players = g.Select(x => new PlayerViewModel
                    {
                        IdentificationNumber = x.Player.IdentificationNumber,
                        FirstName = x.Player.FirstName,
                        Phone = x.Player.Phone,
                        CountryName = x.Player.Country?.Name ?? "",
                        GamesCount = x.GamesCount
                    }).ToList()
                })
                .OrderByDescending(g => g.GamesCount)
                .ToList();
        }
    }

    public class GamesCountGroup
    {
        public int GamesCount { get; set; }
        public List<PlayerViewModel> Players { get; set; } = new List<PlayerViewModel>();
    }
}
