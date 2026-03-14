using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;
using CheckersServer.Models;

namespace CheckersServer.Pages.Update
{
    public class UpdatePlayerModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public UpdatePlayerModel(CheckersDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? SelectedPlayerId { get; set; }

        [BindProperty]
        public Player? Player { get; set; }

        public SelectList? PlayerList { get; set; }
        public SelectList? Countries { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadData();
        }

        public async Task<IActionResult> OnPostSelectAsync()
        {
            await LoadData();

            if (SelectedPlayerId.HasValue)
            {
                Player = await _context.Players.FindAsync(SelectedPlayerId.Value);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            await LoadData();

            if (!SelectedPlayerId.HasValue || Player == null)
            {
                return Page();
            }

            // Validate
            if (string.IsNullOrWhiteSpace(Player.FirstName) || Player.FirstName.Length < 2)
            {
                ModelState.AddModelError("Player.FirstName", "שם פרטי חייב להכיל לפחות 2 אותיות");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Player.Phone) || Player.Phone.Length != 10 || !Player.Phone.All(char.IsDigit))
            {
                ModelState.AddModelError("Player.Phone", "מספר טלפון חייב להכיל בדיוק 10 ספרות");
                return Page();
            }

            if (Player.CountryId == 0)
            {
                ModelState.AddModelError("Player.CountryId", "יש לבחור מדינה");
                return Page();
            }

            // Update player (not changing IdentificationNumber)
            var existingPlayer = await _context.Players.FindAsync(SelectedPlayerId.Value);
            if (existingPlayer != null)
            {
                existingPlayer.FirstName = Player.FirstName;
                existingPlayer.Phone = Player.Phone;
                existingPlayer.CountryId = Player.CountryId;

                await _context.SaveChangesAsync();
                SuccessMessage = "פרטי השחקן עודכנו בהצלחה!";
            }

            Player = existingPlayer;
            return Page();
        }

        private async Task LoadData()
        {
            // Load player list using LINQ
            var players = await _context.Players
                .OrderBy(p => p.FirstName)
                .Select(p => new { p.PlayerId, Display = $"{p.FirstName} (מזהה: {p.IdentificationNumber})" })
                .ToListAsync();

            PlayerList = new SelectList(players, "PlayerId", "Display");

            // Load countries
            var countries = await _context.Countries.OrderBy(c => c.Name).ToListAsync();
            Countries = new SelectList(countries, "CountryId", "Name");
        }
    }
}
