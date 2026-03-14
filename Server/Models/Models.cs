using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckersServer.Models
{
    /// <summary>
    /// Represents a country in the system
    /// </summary>
    public class Country
    {
        [Key]
        public int CountryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
    }

    /// <summary>
    /// Represents a player in the game system
    /// </summary>
    public class Player
    {
        [Key]
        public int PlayerId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "השם חייב להכיל לפחות 2 אותיות")]
        [Display(Name = "שם פרטי")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000, ErrorMessage = "המספר המזהה חייב להיות בין 1 ל-1000")]
        [Display(Name = "מספר מזהה")]
        public int IdentificationNumber { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "מספר טלפון חייב להכיל בדיוק 10 ספרות")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "מספר טלפון חייב להכיל בדיוק 10 ספרות")]
        [Display(Name = "טלפון")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "מדינה")]
        public int CountryId { get; set; }

        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }

        // Navigation property
        public virtual ICollection<GameSession> Games { get; set; } = new List<GameSession>();
    
        // Many-to-many: players participating in the same game session
        public virtual ICollection<GameParticipant> GameParticipants { get; set; } = new List<GameParticipant>();
}

    /// <summary>
    /// Represents a game session
    /// </summary>
    public class GameSession
    {
        [Key]
        public int GameId { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [ForeignKey("PlayerId")]
        public virtual Player? Player { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public TimeSpan? Duration { get; set; }

        [StringLength(20)]
        public string? Result { get; set; } // "Win", "Loss", "InProgress"

        public int TimeLimitSeconds { get; set; } = 10;

        /// <summary>
        /// AI Difficulty: 1=Easy, 2=Medium, 3=Hard
        /// </summary>
        public int Difficulty { get; set; } = 2;

        // Navigation: all human participants sitting on the same client
        public virtual ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();

        // Navigation property
        public virtual ICollection<GameMove> Moves { get; set; } = new List<GameMove>();
    }


/// <summary>
/// Join table: multiple human participants can be associated to the same game session.
/// They all sit on the same screen (single client) and play in turns.
/// </summary>
public class GameParticipant
{
    [Required]
    public int GameId { get; set; }

    [ForeignKey("GameId")]
    public virtual GameSession? Game { get; set; }

    [Required]
    public int PlayerId { get; set; }

    [ForeignKey("PlayerId")]
    public virtual Player? Player { get; set; }

    /// <summary>
    /// 1..N - the order in which participants play their turns on the same client.
    /// </summary>
    public int TurnOrder { get; set; }
}

    /// <summary>
    /// Represents a single move in a game
    /// </summary>
    public class GameMove
    {
        [Key]
        public int MoveId { get; set; }

        [Required]
        public int GameId { get; set; }

        [ForeignKey("GameId")]
        public virtual GameSession? Game { get; set; }

        public int MoveNumber { get; set; }

        public bool IsPlayerMove { get; set; } // true = player, false = server

        // From position
        public int FromRow { get; set; }
        public int FromCol { get; set; }

        // To position
        public int ToRow { get; set; }
        public int ToCol { get; set; }

        public bool IsCapture { get; set; } // אכילה

        public bool IsBackwardMove { get; set; } // צעד אחורה

        public DateTime MoveTime { get; set; }
    }

    /// <summary>
    /// Tracks which soldiers have used their backward move
    /// </summary>
    public class SoldierBackwardUsed
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }

        [ForeignKey("GameId")]
        public virtual GameSession? Game { get; set; }

        public int Row { get; set; }
        public int Col { get; set; }

        public bool IsPlayer { get; set; } // true = player's piece, false = server's piece
    }

    /// <summary>
    /// DTO for player registration
    /// </summary>
    public class PlayerRegistrationDto
    {
        [Required(ErrorMessage = "שם פרטי הינו שדה חובה")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "השם חייב להכיל לפחות 2 אותיות")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "מספר מזהה הינו שדה חובה")]
        [Range(1, 1000, ErrorMessage = "המספר המזהה חייב להיות בין 1 ל-1000")]
        public int IdentificationNumber { get; set; }

        [Required(ErrorMessage = "מספר טלפון הינו שדה חובה")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "מספר טלפון חייב להכיל בדיוק 10 ספרות")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "יש לבחור מדינה")]
        public int CountryId { get; set; }
    }

    /// <summary>
    /// DTO for game move request
    /// </summary>
    public class MoveRequest
    {
        public int GameId { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
    }

    /// <summary>
    /// DTO for server response move
    /// </summary>
    public class MoveResponse
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ServerFromRow { get; set; }
        public int? ServerFromCol { get; set; }
        public int? ServerToRow { get; set; }
        public int? ServerToCol { get; set; }
        public bool ServerCapture { get; set; }
        public string? GameResult { get; set; } // "PlayerWin", "ServerWin", "Continue"
    }

    /// <summary>
    /// DTO for board state
    /// </summary>
    public class BoardState
    {
        public int[,] Board { get; set; } = new int[8, 4];
        // 0 = empty, 1 = player piece, 2 = server piece
    }
}