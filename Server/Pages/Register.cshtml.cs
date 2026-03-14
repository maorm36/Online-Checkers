using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;
using CheckersServer.Models;

namespace CheckersServer.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly CheckersDbContext _context;

        public RegisterModel(CheckersDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int NumberOfPlayers { get; set; } = 1;

        [BindProperty]
        public List<PlayerRegistrationDto> Players { get; set; } = new List<PlayerRegistrationDto>();

        public List<Country> Countries { get; set; } = new List<Country>();

        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Load countries from database using LINQ
            Countries = await _context.Countries.OrderBy(c => c.Name).ToListAsync();
            
            // Initialize empty player list
            if (Players.Count == 0)
            {
                Players.Add(new PlayerRegistrationDto());
            }
        }

        public async Task<IActionResult> OnPostAsync(string? action)
        {
            // Load countries
            Countries = await _context.Countries.OrderBy(c => c.Name).ToListAsync();

            // If number of players changed, just update the form
            if (action != "register")
            {
                // Adjust the Players list size
                while (Players.Count < NumberOfPlayers)
                {
                    Players.Add(new PlayerRegistrationDto());
                }
                while (Players.Count > NumberOfPlayers)
                {
                    Players.RemoveAt(Players.Count - 1);
                }
                return Page();
            }

            // Validate all players
            var errors = new List<string>();

            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];

                // Validate first name
                if (string.IsNullOrWhiteSpace(player.FirstName) || player.FirstName.Length < 2)
                {
                    errors.Add($"שחקן {i + 1}: שם פרטי חייב להכיל לפחות 2 אותיות");
                }

                // Validate identification number
                if (player.IdentificationNumber < 1 || player.IdentificationNumber > 1000)
                {
                    errors.Add($"שחקן {i + 1}: מספר מזהה חייב להיות בין 1 ל-1000");
                }
                else
                {
                    // Check if ID already exists in database (using LINQ)
                    var existsInDb = await _context.Players
                        .AnyAsync(p => p.IdentificationNumber == player.IdentificationNumber);
                    
                    // Check if ID is duplicated in current form
                    var duplicateInForm = Players
                        .Select((p, idx) => new { Player = p, Index = idx })
                        .Any(x => x.Index != i && x.Player.IdentificationNumber == player.IdentificationNumber);

                    if (existsInDb)
                    {
                        errors.Add($"שחקן {i + 1}: מספר מזהה {player.IdentificationNumber} כבר קיים במערכת");
                    }
                    else if (duplicateInForm)
                    {
                        errors.Add($"שחקן {i + 1}: מספר מזהה {player.IdentificationNumber} מופיע פעמיים בטופס");
                    }
                }

                // Validate phone
                if (string.IsNullOrWhiteSpace(player.Phone) || player.Phone.Length != 10 || !player.Phone.All(char.IsDigit))
                {
                    errors.Add($"שחקן {i + 1}: מספר טלפון חייב להכיל בדיוק 10 ספרות");
                }

                // Validate country
                if (player.CountryId == 0)
                {
                    errors.Add($"שחקן {i + 1}: יש לבחור מדינה");
                }
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return Page();
            }

            // Register all players and associate them to the SAME game (single client, multiple participants)
            // We keep the current project behavior intact:
            // - The WinForms client can still start a game by entering ANY participant's identification number.
            // - The server will detect a "Registered" game for that player and start it instead of creating a new one.
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var registeredPlayers = new List<Player>();

                foreach (var dto in Players)
                {
                    var player = new Player
                    {
                        FirstName = dto.FirstName,
                        IdentificationNumber = dto.IdentificationNumber,
                        Phone = dto.Phone,
                        CountryId = dto.CountryId
                    };

                    _context.Players.Add(player);
                    registeredPlayers.Add(player);
                }

                await _context.SaveChangesAsync();

                // Create a new game session in "Registered" state (will become "InProgress" when the client starts it)
                var game = new GameSession
                {
                    PlayerId = registeredPlayers[0].PlayerId, // keep backward compatibility (primary player)
                    StartTime = DateTime.Now,                 // updated when the game becomes InProgress
                    Result = "Registered",
                    TimeLimitSeconds = 10,
                    Difficulty = 2
                };

                _context.Games.Add(game);
                await _context.SaveChangesAsync();

                // Link all participants to this game, in turn order 1..N
                var participants = registeredPlayers
                    .Select((p, idx) => new GameParticipant
                    {
                        GameId = game.GameId,
                        PlayerId = p.PlayerId,
                        TurnOrder = idx + 1
                    })
                    .ToList();

                _context.GameParticipants.AddRange(participants);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                SuccessMessage =
                    $"ההרשמה הושלמה בהצלחה! קוד משחק: {game.GameId}. " +
                    $"נרשמו {registeredPlayers.Count} שחקנים: {string.Join(", ", registeredPlayers.Select(p => p.FirstName + " (" + p.IdentificationNumber + ")"))}. " +
                    "כדי להתחיל משחק בלקוח, מספיק להזין את מספר המזהה של אחד מהשחקנים שנרשמו (השרת יחבר את כולם לאותו משחק).";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"שגיאה בעת שמירת ההרשמה: {ex.Message}");
                return Page();
            }

            // Reset form

            NumberOfPlayers = 1;
            Players = new List<PlayerRegistrationDto> { new PlayerRegistrationDto() };

            return Page();
        }
    }
}
