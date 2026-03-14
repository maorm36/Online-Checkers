using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;

namespace CheckersServer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public IndexModel(CheckersDbContext context)
        {
            _context = context;
        }

        public int TotalPlayers { get; set; }
        public int TotalGames { get; set; }
        public int TotalCountries { get; set; }

        public async Task OnGetAsync()
        {
            // Using LINQ without loops - count queries
            TotalPlayers = await _context.Players.CountAsync();
            TotalGames = await _context.Games.Where(g => g.Result != "InProgress" && g.Result != "Registered").CountAsync();
            TotalCountries = await _context.Players
                .Select(p => p.CountryId)
                .Distinct()
                .CountAsync();
        }
    }
}
