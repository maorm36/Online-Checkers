using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CheckersServer.Data;
using CheckersServer.Models;

namespace CheckersServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly CheckersDbContext _context;
        private readonly Random _random = new Random();

        public GameController(CheckersDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get list of all countries for registration dropdown
        /// </summary>
        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<Country>>> GetCountries()
        {
            return await _context.Countries.OrderBy(c => c.Name).ToListAsync();
        }

        /// <summary>
        /// Check if identification number is available
        /// </summary>
        [HttpGet("check-id/{id}")]
        public async Task<ActionResult<bool>> CheckIdAvailable(int id)
        {
            var exists = await _context.Players.AnyAsync(p => p.IdentificationNumber == id);
            return !exists;
        }

        /// <summary>
        /// Register new player
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<Player>> RegisterPlayer([FromBody] PlayerRegistrationDto dto)
        {
            if (await _context.Players.AnyAsync(p => p.IdentificationNumber == dto.IdentificationNumber))
            {
                return BadRequest("מספר מזהה כבר קיים במערכת");
            }

            var player = new Player
            {
                FirstName = dto.FirstName,
                IdentificationNumber = dto.IdentificationNumber,
                Phone = dto.Phone,
                CountryId = dto.CountryId
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlayer), new { id = player.PlayerId }, player);
        }

        /// <summary>
        /// Get player by ID
        /// </summary>
        [HttpGet("player/{id}")]
        public async Task<ActionResult<Player>> GetPlayer(int id)
        {
            var player = await _context.Players
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound();
            }

            return player;
        }

        /// <summary>
        /// Get player by identification number
        /// </summary>
        [HttpGet("player/by-identification/{identificationNumber}")]
        public async Task<ActionResult<Player>> GetPlayerByIdentification(int identificationNumber)
        {
            var player = await _context.Players
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.IdentificationNumber == identificationNumber);

            if (player == null)
            {
                return NotFound();
            }

            return player;
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<GameSession>> StartGame([FromBody] StartGameRequest request)
        {
            var player = await _context.Players.FindAsync(request.PlayerId);
            if (player == null)
            {
        return NotFound("שחקן לא נמצא");
            }

            // ✅ "Same game" requirement:
            // If this player was registered (via the website) into a shared game that is not started yet,
            // start THAT existing game instead of creating a new one.
            var registeredGame = await _context.Games
        .Include(g => g.Participants)
        .OrderByDescending(g => g.GameId)
        .FirstOrDefaultAsync(g =>
            g.Result == "Registered" &&
            g.Participants.Any(p => p.PlayerId == request.PlayerId));

            if (registeredGame != null)
            {
        registeredGame.StartTime = DateTime.Now;
        registeredGame.TimeLimitSeconds = request.TimeLimitSeconds;
        registeredGame.Difficulty = request.Difficulty;
        registeredGame.Result = "InProgress";

        await _context.SaveChangesAsync();
        return registeredGame;
            }

            // Default behavior (existing project flow): create a fresh solo game
            var game = new GameSession
            {
        PlayerId = request.PlayerId,
        StartTime = DateTime.Now,
        TimeLimitSeconds = request.TimeLimitSeconds,
        Difficulty = request.Difficulty,
        Result = "InProgress"
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            // Keep participant table consistent even for solo games
            _context.GameParticipants.Add(new GameParticipant
            {
        GameId = game.GameId,
        PlayerId = request.PlayerId,
        TurnOrder = 1
            });
            await _context.SaveChangesAsync();

            return game;
        }

        /// <summary>
        /// Get the list of human participants for a game (the group sitting on the same client).
        /// </summary>
        [HttpGet("participants/{gameId}")]
        public async Task<ActionResult<IEnumerable<Player>>> GetGameParticipants(int gameId)
        {
            var exists = await _context.Games.AnyAsync(g => g.GameId == gameId);
            if (!exists)
            {
        return NotFound("משחק לא נמצא");
            }

            var participants = await _context.GameParticipants
        .Where(gp => gp.GameId == gameId)
        .Include(gp => gp.Player)
            .ThenInclude(p => p!.Country)
        .OrderBy(gp => gp.TurnOrder)
        .ToListAsync();

            return participants
        .Where(gp => gp.Player != null)
        .Select(gp => gp.Player!)
        .ToList();
        }

        /// <summary>
        /// Process a player's move and return server's response
        /// </summary>
        [HttpPost("move")]
        public async Task<ActionResult<MoveResponse>> ProcessMove([FromBody] MoveRequest request)
        {
        var game = await _context.Games
        .Include(g => g.Moves)
        .FirstOrDefaultAsync(g => g.GameId == request.GameId);

        if (game == null)
        {
        return NotFound("משחק לא נמצא");
        }

        if (game.Result != "InProgress")
        {
        return BadRequest("המשחק כבר הסתיים");
        }

        // Get current board state
        int[,] board = GetBoardState(game.GameId);

        // Check if this is a backward move
        bool isBackward = IsBackwardMove(request.FromRow, request.FromCol, request.ToRow, request.ToCol, true);

        // Check if this soldier already used backward move
        if (isBackward)
        {
        // Track the soldier's original position to check if it used backward
        var soldierOriginalPos = GetSoldierOriginalPosition(game.GameId, request.FromRow, request.FromCol, true);

        if (HasUsedBackwardMove(game.GameId, soldierOriginalPos.row, soldierOriginalPos.col, true))
        {
        return Ok(new MoveResponse
        {
        IsValid = false,
        ErrorMessage = "חייל זה כבר השתמש בצעד אחורה שלו"
        });
        }
        }

        // Validate player's move
        if (!ValidateMove(board, request.FromRow, request.FromCol, request.ToRow, request.ToCol, true, isBackward))
        {
        return Ok(new MoveResponse
        {
        IsValid = false,
        ErrorMessage = "מהלך לא חוקי"
        });
        }

        // Check if it's a capture move
        bool isCapture = Math.Abs(request.ToRow - request.FromRow) == 2;

        // Backward move cannot be a capture
        if (isBackward && isCapture)
        {
        return Ok(new MoveResponse
        {
        IsValid = false,
        ErrorMessage = "לא ניתן לבצע אכילה בצעד אחורה"
        });
        }

        // Apply player's move to board
        int piece = board[request.FromRow, request.FromCol];
        board[request.FromRow, request.FromCol] = 0;
        board[request.ToRow, request.ToCol] = piece;

        if (isCapture)
        {
        int midRow = (request.FromRow + request.ToRow) / 2;
        int midCol = (request.FromCol + request.ToCol) / 2;
        board[midRow, midCol] = 0;
        }

        // Save player's move to database
        int moveNumber = game.Moves.Count + 1;
        var playerMove = new GameMove
        {
        GameId = game.GameId,
        MoveNumber = moveNumber,
        IsPlayerMove = true,
        FromRow = request.FromRow,
        FromCol = request.FromCol,
        ToRow = request.ToRow,
        ToCol = request.ToCol,
        IsCapture = isCapture,
        IsBackwardMove = isBackward,
        MoveTime = DateTime.Now
        };
        _context.Moves.Add(playerMove);

        // If backward move, record that this soldier used its backward move
        if (isBackward)
        {
        var soldierOriginalPos = GetSoldierOriginalPosition(game.GameId, request.FromRow, request.FromCol, true);
        var backwardRecord = new SoldierBackwardUsed
        {
        GameId = game.GameId,
        Row = soldierOriginalPos.row,
        Col = soldierOriginalPos.col,
        IsPlayer = true
        };
        _context.SoldierBackwardsUsed.Add(backwardRecord);
        }

        // Check if player won (reached row 0)
        if (request.ToRow == 0)
        {
        game.Result = "Win";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        GameResult = "PlayerWin"
        });
        }

        // Check if server has any valid moves
        if (!HasValidMoves(board, game.GameId, false))
        {
        game.Result = "Win";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        GameResult = "PlayerWin"
        });
        }

        // Generate server's move based on difficulty
        var serverMove = GenerateServerMove(board, game.GameId, game.Difficulty);
        if (serverMove == null)
        {
        game.Result = "Win";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        GameResult = "PlayerWin"
        });
        }

        // Apply server's move to board
        bool serverCapture = Math.Abs(serverMove.Value.toRow - serverMove.Value.fromRow) == 2;
        bool serverBackward = IsBackwardMove(serverMove.Value.fromRow, serverMove.Value.fromCol,
        serverMove.Value.toRow, serverMove.Value.toCol, false);

        board[serverMove.Value.fromRow, serverMove.Value.fromCol] = 0;
        board[serverMove.Value.toRow, serverMove.Value.toCol] = 2;

        if (serverCapture)
        {
        int midRow = (serverMove.Value.fromRow + serverMove.Value.toRow) / 2;
        int midCol = (serverMove.Value.fromCol + serverMove.Value.toCol) / 2;
        board[midRow, midCol] = 0;
        }

        // Save server's move
        moveNumber++;
        var serverMoveEntity = new GameMove
        {
        GameId = game.GameId,
        MoveNumber = moveNumber,
        IsPlayerMove = false,
        FromRow = serverMove.Value.fromRow,
        FromCol = serverMove.Value.fromCol,
        ToRow = serverMove.Value.toRow,
        ToCol = serverMove.Value.toCol,
        IsCapture = serverCapture,
        IsBackwardMove = serverBackward,
        MoveTime = DateTime.Now
        };
        _context.Moves.Add(serverMoveEntity);

        // If server made backward move, record it
        if (serverBackward)
        {
        var soldierOriginalPos = GetSoldierOriginalPosition(game.GameId, serverMove.Value.fromRow, serverMove.Value.fromCol, false);
        var backwardRecord = new SoldierBackwardUsed
        {
        GameId = game.GameId,
        Row = soldierOriginalPos.row,
        Col = soldierOriginalPos.col,
        IsPlayer = false
        };
        _context.SoldierBackwardsUsed.Add(backwardRecord);
        }

        // Check if server won (reached row 7)
        if (serverMove.Value.toRow == 7)
        {
        game.Result = "Loss";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        ServerFromRow = serverMove.Value.fromRow,
        ServerFromCol = serverMove.Value.fromCol,
        ServerToRow = serverMove.Value.toRow,
        ServerToCol = serverMove.Value.toCol,
        ServerCapture = serverCapture,
        GameResult = "ServerWin"
        });
        }

        // Check if player has any valid moves
        if (!HasValidMoves(board, game.GameId, true))
        {
        game.Result = "Loss";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        ServerFromRow = serverMove.Value.fromRow,
        ServerFromCol = serverMove.Value.fromCol,
        ServerToRow = serverMove.Value.toRow,
        ServerToCol = serverMove.Value.toCol,
        ServerCapture = serverCapture,
        GameResult = "ServerWin"
        });
        }

        await _context.SaveChangesAsync();

        return Ok(new MoveResponse
        {
        IsValid = true,
        ServerFromRow = serverMove.Value.fromRow,
        ServerFromCol = serverMove.Value.fromCol,
        ServerToRow = serverMove.Value.toRow,
        ServerToCol = serverMove.Value.toCol,
        ServerCapture = serverCapture,
        GameResult = "Continue"
        });
        }

        /// <summary>
        /// Handle timeout - player loses
        /// </summary>
        [HttpPost("timeout/{gameId}")]
        public async Task<ActionResult> HandleTimeout(int gameId)
        {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null)
        {
        return NotFound();
        }

        game.Result = "Loss";
        game.EndTime = DateTime.Now;
        game.Duration = game.EndTime - game.StartTime;
        await _context.SaveChangesAsync();

        return Ok();
        }

        /// <summary>
        /// Get all moves for a game (for replay)
        /// </summary>
        [HttpGet("moves/{gameId}")]
        public async Task<ActionResult<IEnumerable<GameMove>>> GetGameMoves(int gameId)
        {
        var moves = await _context.Moves
        .Where(m => m.GameId == gameId)
        .OrderBy(m => m.MoveNumber)
        .ToListAsync();

        return moves;
        }

        // ============ Private Helper Methods ============

        /// <summary>
        /// Check if a move is a backward move
        /// </summary>
        private bool IsBackwardMove(int fromRow, int fromCol, int toRow, int toCol, bool isPlayer)
        {
        int rowDiff = toRow - fromRow;
        // Player moves up (negative = forward), Server moves down (positive = forward)
        int forwardDir = isPlayer ? -1 : 1;

        // If moving in opposite direction of forward, it's backward
        return rowDiff == -forwardDir && Math.Abs(toCol - fromCol) == 1;
        }

        /// <summary>
        /// Track the original starting position of a soldier by following moves backwards
        /// </summary>
        private (int row, int col) GetSoldierOriginalPosition(int gameId, int currentRow, int currentCol, bool isPlayer)
        {
        // Get all moves for this game
        var moves = _context.Moves
        .Where(m => m.GameId == gameId && m.IsPlayerMove == isPlayer)
        .OrderByDescending(m => m.MoveNumber)
        .ToList();

        int row = currentRow;
        int col = currentCol;

        // Trace back through moves to find original position
        foreach (var move in moves)
        {
        if (move.ToRow == row && move.ToCol == col)
        {
        row = move.FromRow;
        col = move.FromCol;
        }
        }

        return (row, col);
        }

        /// <summary>
        /// Check if a soldier (by original position) has already used its backward move
        /// </summary>
        private bool HasUsedBackwardMove(int gameId, int originalRow, int originalCol, bool isPlayer)
        {
        return _context.SoldierBackwardsUsed.Any(s =>
        s.GameId == gameId &&
        s.Row == originalRow &&
        s.Col == originalCol &&
        s.IsPlayer == isPlayer);
        }

        /// <summary>
        /// Get current board state by replaying all moves
        /// </summary>
        private int[,] GetBoardState(int gameId)
        {
        int[,] board = new int[8, 4];

        // Initial setup - Server pieces at top (rows 0,1,2)
        for (int row = 0; row < 3; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        board[row, col] = 2;
        }
        }

        // Player pieces at bottom (rows 5,6,7)
        for (int row = 5; row < 8; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        board[row, col] = 1;
        }
        }

        // Apply all moves from database
        var moves = _context.Moves
        .Where(m => m.GameId == gameId)
        .OrderBy(m => m.MoveNumber)
        .ToList();

        foreach (var move in moves)
        {
        int piece = board[move.FromRow, move.FromCol];
        board[move.FromRow, move.FromCol] = 0;
        board[move.ToRow, move.ToCol] = piece;

        if (move.IsCapture)
        {
        int midRow = (move.FromRow + move.ToRow) / 2;
        int midCol = (move.FromCol + move.ToCol) / 2;
        board[midRow, midCol] = 0;
        }
        }

        return board;
        }

        /// <summary>
        /// Validate if a move is legal
        /// </summary>
        private bool ValidateMove(int[,] board, int fromRow, int fromCol, int toRow, int toCol, bool isPlayer, bool isBackward)
        {
        // Check bounds
        if (fromRow < 0 || fromRow >= 8 || fromCol < 0 || fromCol >= 4 ||
        toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 4)
        {
        return false;
        }

        // Check if there's a piece at the source
        int expectedPiece = isPlayer ? 1 : 2;
        if (board[fromRow, fromCol] != expectedPiece)
        {
        return false;
        }

        // Check if destination is empty
        if (board[toRow, toCol] != 0)
        {
        return false;
        }

        int rowDiff = toRow - fromRow;
        int colDiff = Math.Abs(toCol - fromCol);

        // Player moves up (negative row diff), Server moves down (positive row diff)
        int forwardDir = isPlayer ? -1 : 1;

        // Regular move (1 square diagonal forward)
        if (rowDiff == forwardDir && colDiff == 1)
        {
        return true;
        }

        // Backward move (1 square diagonal, opposite direction)
        if (rowDiff == -forwardDir && colDiff == 1)
        {
        // Backward move is validated elsewhere for one-time use
        return true;
        }

        // Capture move (2 squares diagonal forward only)
        if (rowDiff == forwardDir * 2 && colDiff == 2)
        {
        int midRow = (fromRow + toRow) / 2;
        int midCol = (fromCol + toCol) / 2;
        int opponentPiece = isPlayer ? 2 : 1;

        if (board[midRow, midCol] == opponentPiece)
        {
        return true;
        }
        }

        return false;
        }

        /// <summary>
        /// Check if a side has any valid moves
        /// </summary>
        private bool HasValidMoves(int[,] board, int gameId, bool isPlayer)
        {
        int pieceType = isPlayer ? 1 : 2;
        int forwardDir = isPlayer ? -1 : 1;

        for (int row = 0; row < 8; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        if (board[row, col] != pieceType) continue;

        // Check forward moves
        foreach (int dc in new[] { -1, 1 })
        {
        int newRow = row + forwardDir;
        int newCol = col + dc;
        if (IsValidCell(newRow, newCol) && board[newRow, newCol] == 0)
        {
        return true;
        }
        }

        // Check backward moves (only if soldier hasn't used it yet)
        var originalPos = GetSoldierOriginalPosition(gameId, row, col, isPlayer);
        if (!HasUsedBackwardMove(gameId, originalPos.row, originalPos.col, isPlayer))
        {
        foreach (int dc in new[] { -1, 1 })
        {
        int newRow = row - forwardDir;
        int newCol = col + dc;
        if (IsValidCell(newRow, newCol) && board[newRow, newCol] == 0)
        {
        return true;
        }
        }
        }

        // Check capture moves (forward only)
        foreach (int dc in new[] { -1, 1 })
        {
        int captureRow = row + forwardDir * 2;
        int captureCol = col + dc * 2;
        int midRow = row + forwardDir;
        int midCol = col + dc;

        int opponentPiece = isPlayer ? 2 : 1;
        if (IsValidCell(captureRow, captureCol) && board[captureRow, captureCol] == 0 &&
        IsValidCell(midRow, midCol) && board[midRow, midCol] == opponentPiece)
        {
        return true;
        }
        }
        }
        }

        return false;
        }

        /// <summary>
        /// Generate a move for the server based on difficulty level
        /// </summary>
        private (int fromRow, int fromCol, int toRow, int toCol)? GenerateServerMove(int[,] board, int gameId, int difficulty)
        {
        var allMoves = GetAllServerMoves(board, gameId);
        if (allMoves.Count == 0) return null;

        return difficulty switch
        {
        1 => GenerateEasyMove(allMoves),
        2 => GenerateMediumMove(board, allMoves),
        3 => GenerateHardMove(board, gameId, allMoves),
        _ => GenerateMediumMove(board, allMoves)
        };
        }

        /// <summary>
        /// Get all valid moves for the server
        /// </summary>
        private List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)> GetAllServerMoves(int[,] board, int gameId)
        {
        var moves = new List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)>();
        int forwardDir = 1;

        for (int row = 0; row < 8; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        if (board[row, col] != 2) continue;

        // Forward moves
        foreach (int dc in new[] { -1, 1 })
        {
        int newRow = row + forwardDir;
        int newCol = col + dc;
        if (IsValidCell(newRow, newCol) && board[newRow, newCol] == 0)
        moves.Add((row, col, newRow, newCol, false));
        }

        // Backward moves (if allowed)
        var originalPos = GetSoldierOriginalPosition(gameId, row, col, false);
        if (!HasUsedBackwardMove(gameId, originalPos.row, originalPos.col, false))
        {
        foreach (int dc in new[] { -1, 1 })
        {
        int newRow = row - forwardDir;
        int newCol = col + dc;
        if (IsValidCell(newRow, newCol) && board[newRow, newCol] == 0)
        moves.Add((row, col, newRow, newCol, false));
        }
        }

        // Capture moves
        foreach (int dc in new[] { -1, 1 })
        {
        int captureRow = row + forwardDir * 2;
        int captureCol = col + dc * 2;
        int midRow = row + forwardDir;
        int midCol = col + dc;

        if (IsValidCell(captureRow, captureCol) && board[captureRow, captureCol] == 0 &&
        IsValidCell(midRow, midCol) && board[midRow, midCol] == 1)
        moves.Add((row, col, captureRow, captureCol, true));
        }
        }
        }
        return moves;
        }

        /// <summary>
        /// Easy: Completely random move (might miss captures)
        /// </summary>
        private (int, int, int, int)? GenerateEasyMove(List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)> moves)
        {
        if (moves.Count == 0) return null;
        var m = moves[_random.Next(moves.Count)];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        /// <summary>
        /// Medium: Prefers captures, advances pieces, avoids obvious danger
        /// </summary>
        private (int, int, int, int)? GenerateMediumMove(int[,] board, List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)> moves)
        {
        if (moves.Count == 0) return null;

        // Priority 1: Capture moves
        var captures = moves.Where(m => m.isCapture).ToList();
        if (captures.Count > 0)
        {
        var m = captures[_random.Next(captures.Count)];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        // Priority 2: Winning move (reach row 7)
        var winMoves = moves.Where(m => m.toRow == 7).ToList();
        if (winMoves.Count > 0)
        {
        var m = winMoves[_random.Next(winMoves.Count)];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        // Priority 3: Safe forward moves (not putting piece in danger)
        var safeMoves = moves.Where(m => !IsMoveInDanger(board, m.toRow, m.toCol)).ToList();
        var safeForward = safeMoves.Where(m => m.toRow > m.fromRow).ToList();
        if (safeForward.Count > 0)
        {
        var m = safeForward[_random.Next(safeForward.Count)];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        // Priority 4: Any safe move
        if (safeMoves.Count > 0)
        {
        var m = safeMoves[_random.Next(safeMoves.Count)];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        // Fallback: Random move
        var move = moves[_random.Next(moves.Count)];
        return (move.fromRow, move.fromCol, move.toRow, move.toCol);
        }

        /// <summary>
        /// Hard: Uses minimax with evaluation function
        /// </summary>
        private (int, int, int, int)? GenerateHardMove(int[,] board, int gameId, List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)> moves)
        {
        if (moves.Count == 0) return null;

        // Check for immediate win
        var winMoves = moves.Where(m => m.toRow == 7).ToList();
        if (winMoves.Count > 0)
        {
        var m = winMoves[0];
        return (m.fromRow, m.fromCol, m.toRow, m.toCol);
        }

        // Evaluate each move with minimax (depth 3)
        (int, int, int, int)? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in moves)
        {
        int[,] newBoard = CloneBoard(board);
        ApplyMove(newBoard, move.fromRow, move.fromCol, move.toRow, move.toCol, move.isCapture);

        int score = Minimax(newBoard, 2, false, int.MinValue, int.MaxValue);

        // Add small random factor to avoid always same moves
        score += _random.Next(-5, 6);

        if (score > bestScore)
        {
        bestScore = score;
        bestMove = (move.fromRow, move.fromCol, move.toRow, move.toCol);
        }
        }

        return bestMove ?? GenerateMediumMove(board, moves);
        }

        /// <summary>
        /// Minimax algorithm with alpha-beta pruning
        /// </summary>
        private int Minimax(int[,] board, int depth, bool isServerTurn, int alpha, int beta)
        {
        // Terminal conditions
        if (depth == 0) return EvaluateBoard(board);

        // Check for wins
        if (HasPieceInRow(board, 2, 7)) return 1000 + depth; // Server wins
        if (HasPieceInRow(board, 1, 0)) return -1000 - depth; // Player wins

        if (isServerTurn)
        {
        int maxEval = int.MinValue;
        var moves = GetSimpleMoves(board, false);
        if (moves.Count == 0) return -500; // Server has no moves

        foreach (var move in moves)
        {
        int[,] newBoard = CloneBoard(board);
        ApplyMove(newBoard, move.fromRow, move.fromCol, move.toRow, move.toCol, move.isCapture);
        int eval = Minimax(newBoard, depth - 1, false, alpha, beta);
        maxEval = Math.Max(maxEval, eval);
        alpha = Math.Max(alpha, eval);
        if (beta <= alpha) break;
        }
        return maxEval;
        }
        else
        {
        int minEval = int.MaxValue;
        var moves = GetSimpleMoves(board, true);
        if (moves.Count == 0) return 500; // Player has no moves

        foreach (var move in moves)
        {
        int[,] newBoard = CloneBoard(board);
        ApplyMove(newBoard, move.fromRow, move.fromCol, move.toRow, move.toCol, move.isCapture);
        int eval = Minimax(newBoard, depth - 1, true, alpha, beta);
        minEval = Math.Min(minEval, eval);
        beta = Math.Min(beta, eval);
        if (beta <= alpha) break;
        }
        return minEval;
        }
        }

        /// <summary>
        /// Evaluate board position (positive = good for server)
        /// </summary>
        private int EvaluateBoard(int[,] board)
        {
        int score = 0;

        for (int row = 0; row < 8; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        if (board[row, col] == 2) // Server piece
        {
        score += 10; // Base piece value
        score += row * 3; // Advancement bonus (closer to row 7)
        if (row == 7) score += 100; // Reached goal
        if (col == 0 || col == 3) score += 2; // Edge protection
        }
        else if (board[row, col] == 1) // Player piece
        {
        score -= 10; // Base piece value
        score -= (7 - row) * 3; // Advancement penalty
        if (row == 0) score -= 100; // Player reached goal
        if (col == 0 || col == 3) score -= 2;
        }
        }
        }

        return score;
        }

        /// <summary>
        /// Check if a move puts the piece in danger of being captured
        /// </summary>
        private bool IsMoveInDanger(int[,] board, int row, int col)
        {
        // Check if player can capture this position
        foreach (int dc in new[] { -1, 1 })
        {
        int playerRow = row + 1; // Player is below
        int playerCol = col + dc;
        int jumpRow = row - 1;
        int jumpCol = col - dc;

        if (IsValidCell(playerRow, playerCol) && board[playerRow, playerCol] == 1 &&
        IsValidCell(jumpRow, jumpCol) && board[jumpRow, jumpCol] == 0)
        return true;
        }
        return false;
        }

        /// <summary>
        /// Get simple moves without backward tracking (for minimax)
        /// </summary>
        private List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)> GetSimpleMoves(int[,] board, bool isPlayer)
        {
        var moves = new List<(int fromRow, int fromCol, int toRow, int toCol, bool isCapture)>();
        int piece = isPlayer ? 1 : 2;
        int forwardDir = isPlayer ? -1 : 1;

        for (int row = 0; row < 8; row++)
        {
        for (int col = 0; col < 4; col++)
        {
        if (board[row, col] != piece) continue;

        // Forward moves
        foreach (int dc in new[] { -1, 1 })
        {
        int newRow = row + forwardDir;
        int newCol = col + dc;
        if (IsValidCell(newRow, newCol) && board[newRow, newCol] == 0)
        moves.Add((row, col, newRow, newCol, false));
        }

        // Capture moves
        int oppPiece = isPlayer ? 2 : 1;
        foreach (int dc in new[] { -1, 1 })
        {
        int captureRow = row + forwardDir * 2;
        int captureCol = col + dc * 2;
        int midRow = row + forwardDir;
        int midCol = col + dc;

        if (IsValidCell(captureRow, captureCol) && board[captureRow, captureCol] == 0 &&
        IsValidCell(midRow, midCol) && board[midRow, midCol] == oppPiece)
        moves.Add((row, col, captureRow, captureCol, true));
        }
        }
        }
        return moves;
        }

        private int[,] CloneBoard(int[,] board)
        {
        int[,] clone = new int[8, 4];
        for (int r = 0; r < 8; r++)
        for (int c = 0; c < 4; c++)
        clone[r, c] = board[r, c];
        return clone;
        }

        private void ApplyMove(int[,] board, int fromRow, int fromCol, int toRow, int toCol, bool isCapture)
        {
        board[toRow, toCol] = board[fromRow, fromCol];
        board[fromRow, fromCol] = 0;
        if (isCapture)
        board[(fromRow + toRow) / 2, (fromCol + toCol) / 2] = 0;
        }

        private bool HasPieceInRow(int[,] board, int piece, int row)
        {
        for (int col = 0; col < 4; col++)
        if (board[row, col] == piece) return true;
        return false;
        }

        private bool IsValidCell(int row, int col)
        {
        return row >= 0 && row < 8 && col >= 0 && col < 4;
        }
        }

        // Request DTOs
        public class StartGameRequest
        {
        public int PlayerId { get; set; }
        public int TimeLimitSeconds { get; set; } = 10;
        public int Difficulty { get; set; } = 2; // 1=Easy, 2=Medium, 3=Hard
        }
        }
