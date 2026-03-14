using System.Drawing.Drawing2D;
using System.Linq;
using Newtonsoft.Json;

namespace CheckersClient
{
    public partial class MainForm : Form
    {
        // Constants
        private const int ROWS = 8;
        private const int COLS = 4;
        private const int CELL_SIZE = 55;
        private const int BOARD_OFFSET_X = 25;
        private const int BOARD_OFFSET_Y = 20;
        private const int ANIMATION_FRAMES = 15;
        private const string SERVER_URL = "https://localhost:7246";

        // Colors
        private readonly Color COLOR_CELL_DARK = Color.FromArgb(139, 90, 43);
        private readonly Color COLOR_CELL_LIGHT = Color.FromArgb(255, 220, 180);
        private readonly Color COLOR_CELL_BORDER = Color.FromArgb(100, 70, 30);
        private readonly Color COLOR_PLAYER_PIECE = Color.FromArgb(25, 25, 112);
        private readonly Color COLOR_SERVER_PIECE = Color.FromArgb(139, 0, 0);
        private readonly Color COLOR_PIECE_HIGHLIGHT = Color.White;
        private readonly Color COLOR_PIECE_BORDER = Color.FromArgb(20, 20, 50);
        private readonly Color COLOR_SELECTED = Color.Yellow;
        private readonly Color COLOR_POSSIBLE_MOVE = Color.FromArgb(100, 0, 255, 0);
        private readonly Color COLOR_WINNER_BLINK = Color.LimeGreen;
        private readonly Color COLOR_DRAWING = Color.Red;
        private readonly Color COLOR_TITLE = Color.FromArgb(25, 25, 112);

        // Fields
        private Rectangle[,] boardCells = null!;
        private int[,] board = null!;
        private int? selectedRow;
        private int? selectedCol;
        private int currentGameId;
        private int localGameId;
        private bool isGameActive;
        private bool isMyTurn;
        private int playerId;
        private string playerName = "";
        private int timeLimitSeconds = 10;
        private int remainingSeconds = 10;
        private int difficultyLevel = 2; // 1=Easy, 2=Medium, 3=Hard
        private bool isAnimating;
        private Point animationStart;
        private Point animationEnd;
        private Point animationCurrent;
        private int animationFrame;
        private bool animatingPlayerPiece = true;
        private bool isBlinking;
        private int blinkCount;
        private bool blinkState;
        private bool playerWon;
        private Bitmap drawingBitmap = null!;
        private bool isDrawing;
        private Point lastDrawPoint;
        private int drawingWidth = 3;
        private HttpClient httpClient = null!;
        private LocalGameDatabase localDb = null!;

        public MainForm()
        {
            InitializeComponent();

            // Enable double buffering on pnlBoard to prevent flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnlBoard, new object[] { true });

            InitializeGame();
        }

        private void InitializeGame()
        {
            boardCells = new Rectangle[ROWS, COLS];
            board = new int[ROWS, COLS];
            httpClient = new HttpClient();
            localDb = new LocalGameDatabase();

            gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            gameTimer.Tick += GameTimer_Tick;

            animationTimer = new System.Windows.Forms.Timer { Interval = 30 };
            animationTimer.Tick += AnimationTimer_Tick;

            blinkTimer = new System.Windows.Forms.Timer { Interval = 200 };
            blinkTimer.Tick += BlinkTimer_Tick;

            for (int row = 0; row < ROWS; row++)
                for (int col = 0; col < COLS; col++)
                    boardCells[row, col] = new Rectangle(
                        BOARD_OFFSET_X + col * CELL_SIZE,
                        BOARD_OFFSET_Y + row * CELL_SIZE,
                        CELL_SIZE, CELL_SIZE);

            drawingBitmap = new Bitmap(pnlBoard.Width, pnlBoard.Height);
            using (var g = Graphics.FromImage(drawingBitmap))
                g.Clear(Color.Transparent);

            ResetBoard();

            // Initialize combo boxes with default selections
            if (cmbTimeLimit.Items.Count > 2)
                cmbTimeLimit.SelectedIndex = 2; // 10 seconds

            if (cmbDifficulty.Items.Count > 1)
                cmbDifficulty.SelectedIndex = 1; // Medium

            // Wire panel events
            pnlBoard.Paint += PnlBoard_Paint;
            pnlBoard.MouseClick += PnlBoard_MouseClick;
            pnlBoard.MouseDown += PnlBoard_MouseDown;
            pnlBoard.MouseMove += PnlBoard_MouseMove;
            pnlBoard.MouseUp += PnlBoard_MouseUp;

            // Force initial paint
            pnlBoard.Invalidate();
        }

        private void ResetBoard()
        {
            for (int row = 0; row < ROWS; row++)
                for (int col = 0; col < COLS; col++)
                    board[row, col] = row < 3 ? 2 : (row >= 5 ? 1 : 0);
        }

        #region Event Handlers

        private void cmbTimeLimit_SelectedIndexChanged(object sender, EventArgs e)
        {
            timeLimitSeconds = cmbTimeLimit.SelectedIndex switch { 0 => 2, 1 => 5, 2 => 10, 3 => 15, _ => 10 };
        }

        private void cmbDifficulty_SelectedIndexChanged(object sender, EventArgs e)
        {
            difficultyLevel = cmbDifficulty.SelectedIndex switch { 0 => 1, 1 => 2, 2 => 3, _ => 2 };
        }

        private async void btnNewGame_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIdentificationNumber.Text))
            { MessageBox.Show("נא להזין מספר מזהה", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!int.TryParse(txtIdentificationNumber.Text, out int idNumber))
            { MessageBox.Show("מספר מזהה חייב להיות מספר", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                var resp = await httpClient.GetAsync($"{SERVER_URL}/api/game/player/by-identification/{idNumber}");
                if (!resp.IsSuccessStatusCode)
                { MessageBox.Show("שחקן לא נמצא. נא להירשם באתר תחילה.", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                var player = JsonConvert.DeserializeObject<PlayerDto>(await resp.Content.ReadAsStringAsync());
                if (player == null) return;

                playerId = player.PlayerId;
                playerName = player.FirstName;
                lblPlayerInfo.Text = $"שחקן: {player.FirstName} | מזהה: {player.IdentificationNumber} | מדינה: {player.Country?.Name ?? "לא ידוע"}";

                // Include Difficulty in the request
                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    PlayerId = playerId,
                    TimeLimitSeconds = timeLimitSeconds,
                    Difficulty = difficultyLevel
                }), System.Text.Encoding.UTF8, "application/json");

                var gameResp = await httpClient.PostAsync($"{SERVER_URL}/api/game/start", content);
                if (!gameResp.IsSuccessStatusCode) return;

                var game = JsonConvert.DeserializeObject<GameSessionDto>(await gameResp.Content.ReadAsStringAsync());
                if (game != null)
                {
                    currentGameId = game.GameId;

                    // Get all participants for this game (for multi-player support)
                    var allPlayerNames = await GetGameParticipantNamesAsync(currentGameId);

                    await TryDisplayParticipantsAsync(currentGameId);

                    isGameActive = true;
                    isMyTurn = true;
                    ResetBoard();
                    StartTurnTimer();
                    lblStatus.Text = "תורך! בחר כלי להזזה";
                    lblStatus.ForeColor = Color.DarkGreen;

                    // Save game with all participant names for replay
                    if (allPlayerNames.Count > 1)
                    {
                        localGameId = localDb.StartNewMultiPlayerGame(currentGameId, allPlayerNames, timeLimitSeconds, difficultyLevel);
                    }
                    else
                    {
                        localGameId = localDb.StartNewGame(currentGameId, playerId, playerName, timeLimitSeconds, difficultyLevel);
                    }
                    pnlBoard.Invalidate();
                }
            }
            catch (Exception ex)
            { MessageBox.Show($"שגיאת תקשורת: {ex.Message}", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// Get all participant names for a game (for multi-player support in replay)
        /// </summary>
        private async Task<List<string>> GetGameParticipantNamesAsync(int gameId)
        {
            var names = new List<string>();
            try
            {
                var resp = await httpClient.GetAsync($"{SERVER_URL}/api/game/participants/{gameId}");
                if (resp.IsSuccessStatusCode)
                {
                    var players = JsonConvert.DeserializeObject<List<PlayerDto>>(await resp.Content.ReadAsStringAsync());
                    if (players != null && players.Count > 0)
                    {
                        names = players.Select(p => p.FirstName).ToList();
                    }
                }
            }
            catch { }

            // If no participants found, use current player name
            if (names.Count == 0)
            {
                names.Add(playerName);
            }
            return names;
        }


        /// <summary>
        /// If the game was registered as a shared game (multiple participants on the same client),
        /// show the full participant list. This keeps the existing single-player flow untouched.
        /// </summary>
        private async Task TryDisplayParticipantsAsync(int gameId)
        {
            try
            {
                var resp = await httpClient.GetAsync($"{SERVER_URL}/api/game/participants/{gameId}");
                if (!resp.IsSuccessStatusCode) return;

                var players = JsonConvert.DeserializeObject<List<PlayerDto>>(await resp.Content.ReadAsStringAsync());
                if (players == null || players.Count == 0) return;

                // If there's only 1 participant, keep the original single-line label.
                if (players.Count == 1) return;

                var lines = new List<string> { "שחקנים במשחק (לפי סדר תורות):" };
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    lines.Add($"{i + 1}. {p.FirstName} | מזהה: {p.IdentificationNumber} | מדינה: {p.Country?.Name ?? "לא ידוע"}");
                }

                lblPlayerInfo.Text = string.Join(Environment.NewLine, lines);
            }
            catch
            {
                // Non-critical: if this fails, we still allow the game to run.
            }
        }

        private void btnReplay_Click(object sender, EventArgs e)
        {
            using var form = new ReplayForm(localDb);
            form.ShowDialog();
        }

        private void btnClearDrawing_Click(object sender, EventArgs e)
        {
            using var g = Graphics.FromImage(drawingBitmap);
            g.Clear(Color.Transparent);
            pnlBoard.Invalidate();
        }

        #endregion

        #region Timer

        private void StartTurnTimer(bool isError = false)
        {
            if (!isError)
            {
                remainingSeconds = timeLimitSeconds;
            }
            UpdateTimeDisplay();
            gameTimer.Start();
        }

        private void StopTurnTimer()
        {
            gameTimer.Stop();
        }

        private async void GameTimer_Tick(object? sender, EventArgs e)
        {
            remainingSeconds--;
            UpdateTimeDisplay();
            if (remainingSeconds <= 0)
            {
                StopTurnTimer();
                isGameActive = false;
                try { await httpClient.PostAsync($"{SERVER_URL}/api/game/timeout/{currentGameId}", null); } catch { }
                localDb.EndGame(localGameId, "Timeout");
                MessageBox.Show("נגמר הזמן! הפסדת את המשחק", "הפסד", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblStatus.Text = "הפסדת - נגמר הזמן";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateTimeDisplay()
        {
            lblTimeRemaining.Text = $"זמן נותר: {remainingSeconds}";
            lblTimeRemaining.ForeColor = remainingSeconds <= 5 ? Color.Red : Color.DarkGreen;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            animationFrame++;
            float p = (float)animationFrame / ANIMATION_FRAMES;
            animationCurrent = new Point(
                (int)(animationStart.X + (animationEnd.X - animationStart.X) * p),
                (int)(animationStart.Y + (animationEnd.Y - animationStart.Y) * p));
            if (animationFrame >= ANIMATION_FRAMES) { animationTimer.Stop(); isAnimating = false; }
            pnlBoard.Invalidate();
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            blinkState = !blinkState;
            if (++blinkCount >= 25) { blinkTimer.Stop(); isBlinking = false; }
            pnlBoard.Invalidate();
        }

        #endregion

        #region Paint

        private void PnlBoard_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawBoard(g);
            DrawPieces(g);
            if (isAnimating) DrawAnimatingPiece(g);
            DrawSelection(g);
            g.DrawImage(drawingBitmap, 0, 0);
        }

        private void DrawBoard(Graphics g)
        {
            using var font = new Font("Segoe UI", 9, FontStyle.Bold);
            using var brush = new SolidBrush(COLOR_TITLE);

            for (int c = 0; c < COLS; c++)
                g.DrawString(((char)('A' + c)).ToString(), font, brush,
                    BOARD_OFFSET_X + c * CELL_SIZE + CELL_SIZE / 2 - 5, 3);

            for (int r = 0; r < ROWS; r++)
                g.DrawString((r + 1).ToString(), font, brush,
                    5, BOARD_OFFSET_Y + r * CELL_SIZE + CELL_SIZE / 2 - 8);

            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    var rect = boardCells[r, c];
                    using (var b = new SolidBrush((r + c) % 2 == 0 ? COLOR_CELL_DARK : COLOR_CELL_LIGHT))
                        g.FillRectangle(b, rect);
                    using (var p = new Pen(COLOR_CELL_BORDER, 1))
                        g.DrawRectangle(p, rect);
                }
        }

        private void DrawPieces(Graphics g)
        {
            int size = CELL_SIZE - 16;
            int padding = 8;

            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    if (board[r, c] == 0) continue;
                    if (isAnimating && r == animationStart.Y / CELL_SIZE && c == animationStart.X / CELL_SIZE) continue;

                    var rect = boardCells[r, c];
                    bool isPlayer = board[r, c] == 1;
                    Color color = isPlayer
                        ? (isBlinking && playerWon && blinkState ? COLOR_WINNER_BLINK : COLOR_PLAYER_PIECE)
                        : (isBlinking && !playerWon && blinkState ? COLOR_WINNER_BLINK : COLOR_SERVER_PIECE);
                    DrawPiece(g, rect.X + padding, rect.Y + padding, size, color);
                }
        }

        private void DrawAnimatingPiece(Graphics g)
        {
            int size = CELL_SIZE - 16;
            DrawPiece(g, animationCurrent.X + BOARD_OFFSET_X + 8, animationCurrent.Y + BOARD_OFFSET_Y + 8,
                size, animatingPlayerPiece ? COLOR_PLAYER_PIECE : COLOR_SERVER_PIECE);
        }

        private void DrawPiece(Graphics g, int x, int y, int size, Color color)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(x, y, size, size);
            using (var gradBrush = new PathGradientBrush(path))
            {
                gradBrush.CenterColor = Color.FromArgb(200, COLOR_PIECE_HIGHLIGHT);
                gradBrush.SurroundColors = new[] { color };
                gradBrush.CenterPoint = new PointF(x + size / 3f, y + size / 3f);
                g.FillPath(gradBrush, path);
            }
            using var pen = new Pen(COLOR_PIECE_BORDER, 2);
            g.DrawEllipse(pen, x, y, size, size);
        }

        private void DrawSelection(Graphics g)
        {
            if (!selectedRow.HasValue || !selectedCol.HasValue) return;

            using (var pen = new Pen(COLOR_SELECTED, 4))
                g.DrawRectangle(pen, boardCells[selectedRow.Value, selectedCol.Value]);

            foreach (var (r, c) in GetMoves(selectedRow.Value, selectedCol.Value))
                using (var selBrush = new SolidBrush(COLOR_POSSIBLE_MOVE))
                    g.FillRectangle(selBrush, boardCells[r, c]);
        }

        #endregion

        #region Game Logic

        private List<(int, int)> GetMoves(int fr, int fc)
        {
            var m = new List<(int, int)>();
            if (board[fr, fc] != 1) return m;

            foreach (int dc in new[] { -1, 1 })
            {
                int nr = fr - 1, nc = fc + dc;
                if (Ok(nr, nc) && board[nr, nc] == 0) m.Add((nr, nc));
            }

            foreach (int dc in new[] { -1, 1 })
            {
                int cr = fr - 2, cc = fc + dc * 2, mr = fr - 1, mc = fc + dc;
                if (Ok(cr, cc) && board[cr, cc] == 0 && Ok(mr, mc) && board[mr, mc] == 2)
                    m.Add((cr, cc));
            }

            foreach (int dc in new[] { -1, 1 })
            {
                int br = fr + 1, bc = fc + dc;
                if (Ok(br, bc) && board[br, bc] == 0) m.Add((br, bc));
            }

            return m;
        }

        private bool Ok(int r, int c) => r >= 0 && r < ROWS && c >= 0 && c < COLS;

        #endregion

        #region Mouse Events

        private async void PnlBoard_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (!isGameActive || !isMyTurn || isAnimating) return;

            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                    if (boardCells[r, c].Contains(e.Location))
                    {
                        await HandleClick(r, c);
                        return;
                    }
        }

        private async Task HandleClick(int row, int col)
        {
            if (!selectedRow.HasValue)
            {
                if (board[row, col] == 1)
                {
                    selectedRow = row;
                    selectedCol = col;
                    pnlBoard.Invalidate();
                }
                return;
            }

            if (board[row, col] == 1)
            {
                selectedRow = row;
                selectedCol = col;
                pnlBoard.Invalidate();
                return;
            }

            if (!selectedCol.HasValue) return;

            var moves = GetMoves(selectedRow.Value, selectedCol.Value);
            if (!moves.Contains((row, col))) return;

            int fr = selectedRow.Value, fc = selectedCol.Value, tr = row, tc = col;
            StopTurnTimer();
            isMyTurn = false;
            lblStatus.Text = "שולח מהלך...";
            lblStatus.RightToLeft = RightToLeft.Yes;

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { GameId = currentGameId, FromRow = fr, FromCol = fc, ToRow = tr, ToCol = tc }), System.Text.Encoding.UTF8, "application/json");
                var resp = await httpClient.PostAsync($"{SERVER_URL}/api/game/move", content);
                var mr = JsonConvert.DeserializeObject<MoveResponseDto>(await resp.Content.ReadAsStringAsync());

                if (mr == null || !mr.IsValid)
                {
                    isMyTurn = true;

                    MessageBox.Show(mr?.ErrorMessage ?? "מהלך לא חוקי", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    lblStatus.Text = "תורך! בחר כלי להזזה";
                    lblStatus.ForeColor = Color.DarkGreen;

                    StartTurnTimer(true);
                    pnlBoard.Invalidate();
                    return;
                }

                animatingPlayerPiece = true;
                await Animate(fr, fc, tr, tc);
                if (Math.Abs(tr - fr) == 2) board[(fr + tr) / 2, (fc + tc) / 2] = 0;
                board[fr, fc] = 0; board[tr, tc] = 1;
                localDb.RecordMove(localGameId, fr, fc, tr, tc, true, Math.Abs(tr - fr) == 2);
                selectedRow = null; selectedCol = null;

                if (mr.GameResult == "PlayerWin")
                {
                    isGameActive = false; localDb.EndGame(localGameId, "PlayerWin");
                    StartBlink(true); lblStatus.Text = "ניצחת! 🎉"; lblStatus.ForeColor = Color.DarkGreen; return;
                }

                if (mr.ServerFromRow.HasValue && mr.ServerFromCol.HasValue && mr.ServerToRow.HasValue && mr.ServerToCol.HasValue)
                {
                    lblStatus.Text = "תור השרת...";
                    lblStatus.RightToLeft = RightToLeft.Yes;
                    await Task.Delay(500);
                    animatingPlayerPiece = false;
                    await Animate(mr.ServerFromRow.Value, mr.ServerFromCol.Value, mr.ServerToRow.Value, mr.ServerToCol.Value);
                    if (mr.ServerCapture) board[(mr.ServerFromRow.Value + mr.ServerToRow.Value) / 2, (mr.ServerFromCol.Value + mr.ServerToCol.Value) / 2] = 0;
                    board[mr.ServerFromRow.Value, mr.ServerFromCol.Value] = 0;
                    board[mr.ServerToRow.Value, mr.ServerToCol.Value] = 2;
                    localDb.RecordMove(localGameId, mr.ServerFromRow.Value, mr.ServerFromCol.Value, mr.ServerToRow.Value, mr.ServerToCol.Value, false, mr.ServerCapture);
                }

                if (mr.GameResult == "ServerWin")
                {
                    isGameActive = false; localDb.EndGame(localGameId, "ServerWin");
                    StartBlink(false); lblStatus.Text = "הפסדת!"; lblStatus.ForeColor = Color.Red; return;
                }

                isMyTurn = true; lblStatus.Text = "תורך! בחר כלי להזזה"; lblStatus.ForeColor = Color.DarkGreen;
                StartTurnTimer(); pnlBoard.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאת תקשורת: {ex.Message}", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isMyTurn = true; StartTurnTimer();
            }
        }

        private async Task Animate(int fr, int fc, int tr, int tc)
        {
            animationStart = new Point(fc * CELL_SIZE, fr * CELL_SIZE);
            animationEnd = new Point(tc * CELL_SIZE, tr * CELL_SIZE);
            animationCurrent = animationStart;
            animationFrame = 0;
            isAnimating = true;
            animationTimer.Start();
            while (isAnimating) await Task.Delay(10);
        }

        private void StartBlink(bool playerWins)
        {
            playerWon = playerWins;
            isBlinking = true;
            blinkState = false;
            blinkCount = 0;
            blinkTimer.Start();
        }

        private void PnlBoard_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && IsPointOnBoard(e.Location))
            {
                isDrawing = true;
                lastDrawPoint = e.Location;
            }
        }

        private void PnlBoard_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isDrawing && IsPointOnBoard(e.Location))
            {
                using (var g = Graphics.FromImage(drawingBitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var pen = new Pen(COLOR_DRAWING, drawingWidth))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, lastDrawPoint, e.Location);
                    }
                }
                lastDrawPoint = e.Location;
                pnlBoard.Invalidate();
            }
        }

        private void PnlBoard_MouseUp(object? sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private bool IsPointOnBoard(Point p)
        {
            int boardLeft = BOARD_OFFSET_X;
            int boardTop = BOARD_OFFSET_Y;
            int boardRight = BOARD_OFFSET_X + COLS * CELL_SIZE;
            int boardBottom = BOARD_OFFSET_Y + ROWS * CELL_SIZE;

            return p.X >= boardLeft && p.X <= boardRight &&
                   p.Y >= boardTop && p.Y <= boardBottom;
        }

        #endregion
    }

    #region DTOs
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string FirstName { get; set; } = "";
        public int IdentificationNumber { get; set; }
        public string Phone { get; set; } = "";
        public CountryDto? Country { get; set; }
    }

    public class CountryDto
    {
        public int CountryId { get; set; }
        public string Name { get; set; } = "";
    }

    public class GameSessionDto
    {
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartTime { get; set; }
        public string Result { get; set; } = "";
    }

    public class MoveResponseDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GameResult { get; set; }
        public int? ServerFromRow { get; set; }
        public int? ServerFromCol { get; set; }
        public int? ServerToRow { get; set; }
        public int? ServerToCol { get; set; }
        public bool ServerCapture { get; set; }
    }
    #endregion
}