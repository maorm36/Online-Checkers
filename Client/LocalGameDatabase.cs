using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace CheckersClient
{
    /// <summary>
    /// מחלקה לניהול מסד נתונים מקומי לשמירת משחקים והפעלת חזרות
    /// Local database class for storing games and replay functionality
    /// </summary>
    public class LocalGameDatabase
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private int _currentMoveNumber = 0;

        public LocalGameDatabase()
        {
            // שימוש ב-LocalDB למסד נתונים מקומי
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "CheckersGame");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _databasePath = Path.Combine(appFolder, "CheckersReplay.mdf");
            _connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={_databasePath};Integrated Security=True;Connect Timeout=30";

            InitializeDatabase();
        }

        /// <summary>
        /// אתחול מסד הנתונים - יצירת טבלאות אם לא קיימות
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                // יצירת מסד נתונים אם לא קיים
                string masterConnection = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30";

                if (!File.Exists(_databasePath))
                {
                    using (var connection = new SqlConnection(masterConnection))
                    {
                        connection.Open();
                        string createDb = $@"
                            CREATE DATABASE CheckersReplayDb
                            ON PRIMARY (
                                NAME = CheckersReplayDb_Data,
                                FILENAME = '{_databasePath}'
                            )
                            LOG ON (
                                NAME = CheckersReplayDb_Log,
                                FILENAME = '{_databasePath.Replace(".mdf", "_log.ldf")}'
                            )";

                        using (var cmd = new SqlCommand(createDb, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // יצירת טבלאות
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // טבלת משחקים - עם עמודת Difficulty
                    string createGamesTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocalGames')
                        BEGIN
                            CREATE TABLE LocalGames (
                                LocalGameId INT IDENTITY(1,1) PRIMARY KEY,
                                ServerGameId INT NULL,
                                PlayerName NVARCHAR(100) NOT NULL,
                                StartTime DATETIME NOT NULL,
                                EndTime DATETIME NULL,
                                Result NVARCHAR(50) NULL,
                                TimeLimitSeconds INT NOT NULL,
                                Difficulty INT NOT NULL DEFAULT 2
                            )
                        END";

                    using (var cmd = new SqlCommand(createGamesTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // הוספת עמודת Difficulty אם לא קיימת (עבור מסדי נתונים קיימים)
                    string addDifficultyColumn = @"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LocalGames') AND name = 'Difficulty')
                        BEGIN
                            ALTER TABLE LocalGames ADD Difficulty INT NOT NULL DEFAULT 2
                        END";

                    using (var cmd = new SqlCommand(addDifficultyColumn, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // הוספת עמודות לתמיכה במשחק קבוצתי
                    string addMultiPlayerColumns = @"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LocalGames') AND name = 'AllPlayerNames')
                        BEGIN
                            ALTER TABLE LocalGames ADD AllPlayerNames NVARCHAR(500) NULL
                        END;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LocalGames') AND name = 'TotalPlayers')
                        BEGIN
                            ALTER TABLE LocalGames ADD TotalPlayers INT NOT NULL DEFAULT 1
                        END";

                    using (var cmd = new SqlCommand(addMultiPlayerColumns, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // טבלת מהלכים
                    string createMovesTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocalMoves')
                        BEGIN
                            CREATE TABLE LocalMoves (
                                LocalMoveId INT IDENTITY(1,1) PRIMARY KEY,
                                LocalGameId INT NOT NULL,
                                MoveNumber INT NOT NULL,
                                IsPlayerMove BIT NOT NULL,
                                FromRow INT NOT NULL,
                                FromCol INT NOT NULL,
                                ToRow INT NOT NULL,
                                ToCol INT NOT NULL,
                                IsCapture BIT NOT NULL,
                                IsBackwardMove BIT NOT NULL,
                                MoveTime DATETIME NOT NULL,
                                FOREIGN KEY (LocalGameId) REFERENCES LocalGames(LocalGameId) ON DELETE CASCADE
                            )
                        END";

                    using (var cmd = new SqlCommand(createMovesTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // אם יש בעיה עם LocalDB, נשתמש במערך בזיכרון
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                _useInMemory = true;
            }
        }

        private bool _useInMemory = false;
        private List<LocalGame> _inMemoryGames = new List<LocalGame>();
        private List<LocalMove> _inMemoryMoves = new List<LocalMove>();
        private int _nextGameId = 1;
        private int _nextMoveId = 1;

        /// <summary>
        /// התחלת משחק חדש - גרסה עם 5 פרמטרים כולל difficulty (נקראת מ-MainForm)
        /// </summary>
        public int StartNewGame(int serverGameId, int playerId, string playerName, int timeLimitSeconds, int difficulty)
        {
            _currentMoveNumber = 0;
            return StartNewGameInternal(serverGameId, playerName, playerName, 1, timeLimitSeconds, difficulty);
        }

        /// <summary>
        /// התחלת משחק חדש - גרסה עם 4 פרמטרים (תאימות לאחור)
        /// </summary>
        public int StartNewGame(int serverGameId, int playerId, string playerName, int timeLimitSeconds)
        {
            _currentMoveNumber = 0;
            return StartNewGameInternal(serverGameId, playerName, playerName, 1, timeLimitSeconds, 2); // Default Medium
        }

        /// <summary>
        /// התחלת משחק קבוצתי - גרסה עם רשימת שמות שחקנים
        /// </summary>
        public int StartNewMultiPlayerGame(int serverGameId, List<string> playerNames, int timeLimitSeconds, int difficulty)
        {
            _currentMoveNumber = 0;
            string firstPlayer = playerNames.FirstOrDefault() ?? "";
            string allPlayers = string.Join(", ", playerNames);
            return StartNewGameInternal(serverGameId, firstPlayer, allPlayers, playerNames.Count, timeLimitSeconds, difficulty);
        }

        /// <summary>
        /// התחלת משחק חדש - שמירה במסד הנתונים המקומי
        /// </summary>
        private int StartNewGameInternal(int? serverGameId, string playerName, string allPlayerNames, int totalPlayers, int timeLimitSeconds, int difficulty)
        {
            _currentMoveNumber = 0;

            if (_useInMemory)
            {
                var game = new LocalGame
                {
                    LocalGameId = _nextGameId++,
                    ServerGameId = serverGameId,
                    PlayerName = playerName,
                    AllPlayerNames = allPlayerNames,
                    TotalPlayers = totalPlayers,
                    StartTime = DateTime.Now,
                    TimeLimitSeconds = timeLimitSeconds,
                    Difficulty = difficulty
                };
                _inMemoryGames.Add(game);
                return game.LocalGameId;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    INSERT INTO LocalGames (ServerGameId, PlayerName, AllPlayerNames, TotalPlayers, StartTime, TimeLimitSeconds, Difficulty)
                    VALUES (@ServerGameId, @PlayerName, @AllPlayerNames, @TotalPlayers, @StartTime, @TimeLimitSeconds, @Difficulty);
                    SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ServerGameId", serverGameId.HasValue ? (object)serverGameId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlayerName", playerName);
                    cmd.Parameters.AddWithValue("@AllPlayerNames", allPlayerNames ?? playerName);
                    cmd.Parameters.AddWithValue("@TotalPlayers", totalPlayers);
                    cmd.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@TimeLimitSeconds", timeLimitSeconds);
                    cmd.Parameters.AddWithValue("@Difficulty", difficulty);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// הקלטת מהלך בודד - גרסה עם 7 פרמטרים (נקראת מ-MainForm)
        /// </summary>
        public void RecordMove(int localGameId, int fromRow, int fromCol, int toRow, int toCol, bool isPlayerMove, bool isCapture)
        {
            _currentMoveNumber++;
            bool isBackwardMove = isPlayerMove && (toRow > fromRow);
            RecordMove(localGameId, _currentMoveNumber, isPlayerMove, fromRow, fromCol, toRow, toCol, isCapture, isBackwardMove);
        }

        /// <summary>
        /// הקלטת מהלך בודד - גרסה מלאה
        /// </summary>
        public void RecordMove(int localGameId, int moveNumber, bool isPlayerMove, int fromRow, int fromCol, int toRow, int toCol, bool isCapture, bool isBackwardMove)
        {
            if (_useInMemory)
            {
                _inMemoryMoves.Add(new LocalMove
                {
                    LocalMoveId = _nextMoveId++,
                    LocalGameId = localGameId,
                    MoveNumber = moveNumber,
                    IsPlayerMove = isPlayerMove,
                    FromRow = fromRow,
                    FromCol = fromCol,
                    ToRow = toRow,
                    ToCol = toCol,
                    IsCapture = isCapture,
                    IsBackwardMove = isBackwardMove,
                    MoveTime = DateTime.Now
                });
                return;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    INSERT INTO LocalMoves (LocalGameId, MoveNumber, IsPlayerMove, FromRow, FromCol, ToRow, ToCol, IsCapture, IsBackwardMove, MoveTime)
                    VALUES (@LocalGameId, @MoveNumber, @IsPlayerMove, @FromRow, @FromCol, @ToRow, @ToCol, @IsCapture, @IsBackwardMove, @MoveTime)";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@LocalGameId", localGameId);
                    cmd.Parameters.AddWithValue("@MoveNumber", moveNumber);
                    cmd.Parameters.AddWithValue("@IsPlayerMove", isPlayerMove);
                    cmd.Parameters.AddWithValue("@FromRow", fromRow);
                    cmd.Parameters.AddWithValue("@FromCol", fromCol);
                    cmd.Parameters.AddWithValue("@ToRow", toRow);
                    cmd.Parameters.AddWithValue("@ToCol", toCol);
                    cmd.Parameters.AddWithValue("@IsCapture", isCapture);
                    cmd.Parameters.AddWithValue("@IsBackwardMove", isBackwardMove);
                    cmd.Parameters.AddWithValue("@MoveTime", DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// סיום משחק - עדכון התוצאה וזמן הסיום
        /// </summary>
        public void EndGame(int localGameId, string result)
        {
            if (_useInMemory)
            {
                var game = _inMemoryGames.FirstOrDefault(g => g.LocalGameId == localGameId);
                if (game != null)
                {
                    game.EndTime = DateTime.Now;
                    game.Result = result;
                }
                return;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE LocalGames SET EndTime = @EndTime, Result = @Result WHERE LocalGameId = @LocalGameId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@LocalGameId", localGameId);
                    cmd.Parameters.AddWithValue("@EndTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Result", result);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// קבלת כל המשחקים השמורים - שימוש ב-LINQ
        /// </summary>
        public List<LocalGame> GetAllGames()
        {
            if (_useInMemory)
            {
                return _inMemoryGames
                    .Where(g => g.EndTime.HasValue)
                    .OrderByDescending(g => g.StartTime)
                    .ToList();
            }

            var games = new List<LocalGame>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM LocalGames ORDER BY StartTime DESC";

                using (var cmd = new SqlCommand(sql, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        games.Add(new LocalGame
                        {
                            LocalGameId = reader.GetInt32(reader.GetOrdinal("LocalGameId")),
                            ServerGameId = reader.IsDBNull(reader.GetOrdinal("ServerGameId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("ServerGameId")),
                            PlayerName = reader.GetString(reader.GetOrdinal("PlayerName")),
                            AllPlayerNames = GetStringFromReader(reader, "AllPlayerNames"),
                            TotalPlayers = GetIntFromReader(reader, "TotalPlayers", 1),
                            StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                            EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("EndTime")),
                            Result = reader.IsDBNull(reader.GetOrdinal("Result")) ? null : reader.GetString(reader.GetOrdinal("Result")),
                            TimeLimitSeconds = reader.GetInt32(reader.GetOrdinal("TimeLimitSeconds")),
                            Difficulty = GetDifficultyFromReader(reader)
                        });
                    }
                }
            }

            return games
                .Where(g => g.EndTime.HasValue)
                .OrderByDescending(g => g.StartTime)
                .ToList();
        }

        /// <summary>
        /// Helper to safely read string column (for backward compatibility)
        /// </summary>
        private string GetStringFromReader(SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Helper to safely read int column (for backward compatibility)
        /// </summary>
        private int GetIntFromReader(SqlDataReader reader, string columnName, int defaultValue)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Helper to safely read Difficulty column (for backward compatibility)
        /// </summary>
        private int GetDifficultyFromReader(SqlDataReader reader)
        {
            try
            {
                int ordinal = reader.GetOrdinal("Difficulty");
                return reader.IsDBNull(ordinal) ? 2 : reader.GetInt32(ordinal);
            }
            catch
            {
                return 2; // Default to Medium if column doesn't exist
            }
        }

        /// <summary>
        /// קבלת כל המהלכים של משחק מסוים - שימוש ב-LINQ
        /// </summary>
        public List<LocalMove> GetGameMoves(int localGameId)
        {
            if (_useInMemory)
            {
                return _inMemoryMoves
                    .Where(m => m.LocalGameId == localGameId)
                    .OrderBy(m => m.MoveNumber)
                    .ToList();
            }

            var moves = new List<LocalMove>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM LocalMoves WHERE LocalGameId = @LocalGameId ORDER BY MoveNumber";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@LocalGameId", localGameId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            moves.Add(new LocalMove
                            {
                                LocalMoveId = reader.GetInt32(reader.GetOrdinal("LocalMoveId")),
                                LocalGameId = reader.GetInt32(reader.GetOrdinal("LocalGameId")),
                                MoveNumber = reader.GetInt32(reader.GetOrdinal("MoveNumber")),
                                IsPlayerMove = reader.GetBoolean(reader.GetOrdinal("IsPlayerMove")),
                                FromRow = reader.GetInt32(reader.GetOrdinal("FromRow")),
                                FromCol = reader.GetInt32(reader.GetOrdinal("FromCol")),
                                ToRow = reader.GetInt32(reader.GetOrdinal("ToRow")),
                                ToCol = reader.GetInt32(reader.GetOrdinal("ToCol")),
                                IsCapture = reader.GetBoolean(reader.GetOrdinal("IsCapture")),
                                IsBackwardMove = reader.GetBoolean(reader.GetOrdinal("IsBackwardMove")),
                                MoveTime = reader.GetDateTime(reader.GetOrdinal("MoveTime"))
                            });
                        }
                    }
                }
            }

            return moves.OrderBy(m => m.MoveNumber).ToList();
        }

        /// <summary>
        /// מחיקת משחק ישן - מחיקת cascade עם המהלכים
        /// </summary>
        public void DeleteGame(int localGameId)
        {
            if (_useInMemory)
            {
                _inMemoryMoves.RemoveAll(m => m.LocalGameId == localGameId);
                _inMemoryGames.RemoveAll(g => g.LocalGameId == localGameId);
                return;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM LocalGames WHERE LocalGameId = @LocalGameId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@LocalGameId", localGameId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// שאילתת LINQ - קבלת סטטיסטיקות משחקים
        /// </summary>
        public GameStatistics GetStatistics()
        {
            var games = GetAllGames();

            return new GameStatistics
            {
                TotalGames = games.Count,
                PlayerWins = games.Count(g => g.Result == "PlayerWin" || g.Result == "Win"),
                ServerWins = games.Count(g => g.Result == "ServerWin" || g.Result == "Loss"),
                Timeouts = games.Count(g => g.Result == "Timeout"),
                AverageGameDuration = games
                    .Where(g => g.EndTime.HasValue)
                    .Select(g => (g.EndTime!.Value - g.StartTime).TotalSeconds)
                    .DefaultIfEmpty(0)
                    .Average()
            };
        }

        /// <summary>
        /// שאילתת LINQ - קבלת משחקים לפי תוצאה
        /// </summary>
        public List<LocalGame> GetGamesByResult(string result)
        {
            return GetAllGames()
                .Where(g => g.Result == result)
                .OrderByDescending(g => g.StartTime)
                .ToList();
        }

        /// <summary>
        /// שאילתת LINQ - קבלת משחקים לפי רמת קושי
        /// </summary>
        public List<LocalGame> GetGamesByDifficulty(int difficulty)
        {
            return GetAllGames()
                .Where(g => g.Difficulty == difficulty)
                .OrderByDescending(g => g.StartTime)
                .ToList();
        }
    }

    /// <summary>
    /// מודל משחק מקומי
    /// </summary>
    public class LocalGame
    {
        public int LocalGameId { get; set; }
        public int? ServerGameId { get; set; }
        public string PlayerName { get; set; } = "";
        /// <summary>
        /// All player names (comma-separated) for multi-player games
        /// </summary>
        public string AllPlayerNames { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Result { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int Difficulty { get; set; } = 2; // 1=Easy, 2=Medium, 3=Hard
        public int TotalPlayers { get; set; } = 1;

        public string DifficultyName => Difficulty switch
        {
            1 => "קל",
            2 => "בינוני",
            3 => "קשה",
            _ => "בינוני"
        };

        /// <summary>
        /// Display name - shows all players if multi-player, otherwise single player
        /// </summary>
        public string DisplayPlayerNames => !string.IsNullOrEmpty(AllPlayerNames) ? AllPlayerNames : PlayerName;

        public override string ToString() => $"{DisplayPlayerNames} - {StartTime:dd/MM/yyyy HH:mm} - {DifficultyName} - {Result ?? "בתהליך"}";
        public string DisplayText => ToString();
    }

    /// <summary>
    /// מודל מהלך מקומי
    /// </summary>
    public class LocalMove
    {
        public int LocalMoveId { get; set; }
        public int LocalGameId { get; set; }
        public int MoveNumber { get; set; }
        public bool IsPlayerMove { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsCapture { get; set; }
        public bool IsBackwardMove { get; set; }
        public DateTime MoveTime { get; set; }

        public string DisplayText =>
            $"מהלך {MoveNumber}: {(IsPlayerMove ? "שחקן" : "שרת")} - ({FromRow},{FromCol}) → ({ToRow},{ToCol})" +
            $"{(IsCapture ? " [אכילה]" : "")}{(IsBackwardMove ? " [אחורה]" : "")}";
    }

    /// <summary>
    /// מודל סטטיסטיקות
    /// </summary>
    public class GameStatistics
    {
        public int TotalGames { get; set; }
        public int PlayerWins { get; set; }
        public int ServerWins { get; set; }
        public int Timeouts { get; set; }
        public double AverageGameDuration { get; set; }

        public double PlayerWinPercentage => TotalGames > 0 ? (PlayerWins * 100.0 / TotalGames) : 0;
        public double ServerWinPercentage => TotalGames > 0 ? (ServerWins * 100.0 / TotalGames) : 0;
    }
}