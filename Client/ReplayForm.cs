using System.Drawing.Drawing2D;

namespace CheckersClient
{
    public partial class ReplayForm : Form
    {
        // Constants
        private const int CELL_SIZE = 55;
        private const int BOARD_ROWS = 8;
        private const int BOARD_COLS = 4;
        private const int BOARD_OFFSET_X = 25;
        private const int BOARD_OFFSET_Y = 20;

        // Colors
        private readonly Color COLOR_CELL_DARK = Color.FromArgb(139, 90, 43);
        private readonly Color COLOR_CELL_LIGHT = Color.FromArgb(255, 220, 180);
        private readonly Color COLOR_CELL_BORDER = Color.FromArgb(100, 70, 30);
        private readonly Color COLOR_PLAYER_PIECE = Color.FromArgb(25, 25, 112);
        private readonly Color COLOR_SERVER_PIECE = Color.FromArgb(139, 0, 0);
        private readonly Color COLOR_PIECE_HIGHLIGHT = Color.White;
        private readonly Color COLOR_PIECE_BORDER = Color.FromArgb(20, 20, 50);
        private readonly Color COLOR_TITLE = Color.FromArgb(50, 50, 100);

        // Fields - use = null! for fields initialized in InitializeReplay()
        private LocalGameDatabase _database = null!;
        private LocalGame? _currentGame;
        private List<LocalMove> _currentMoves = new List<LocalMove>();
        private int _currentMoveIndex = -1;
        private Rectangle[,] boardCells = null!;
        private int[,] board = null!;
        private System.Windows.Forms.Timer playTimer = null!;
        private bool isPlaying;

        public ReplayForm(LocalGameDatabase database)
        {
            InitializeComponent();
            _database = database;

            // Enable double buffering on pnlBoard to prevent flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnlBoard, new object[] { true });

            InitializeReplay();
        }

        private void InitializeReplay()
        {
            boardCells = new Rectangle[BOARD_ROWS, BOARD_COLS];
            board = new int[BOARD_ROWS, BOARD_COLS];

            playTimer = new System.Windows.Forms.Timer { Interval = 800 };
            playTimer.Tick += PlayTimer_Tick;

            for (int r = 0; r < BOARD_ROWS; r++)
                for (int c = 0; c < BOARD_COLS; c++)
                    boardCells[r, c] = new Rectangle(
                        BOARD_OFFSET_X + c * CELL_SIZE,
                        BOARD_OFFSET_Y + r * CELL_SIZE,
                        CELL_SIZE, CELL_SIZE);

            ResetBoard();

            pnlBoard.Paint += PnlBoard_Paint;

            LoadGamesList();
            LoadStatistics();
        }

        private void ResetBoard()
        {
            for (int r = 0; r < BOARD_ROWS; r++)
                for (int c = 0; c < BOARD_COLS; c++)
                    board[r, c] = r < 3 ? 2 : (r >= 5 ? 1 : 0);
        }

        private void LoadGamesList()
        {
            cboGames.Items.Clear();
            foreach (var game in _database.GetAllGames())
                cboGames.Items.Add(game);
            if (cboGames.Items.Count > 0)
                cboGames.SelectedIndex = 0;
        }

        private void LoadStatistics()
        {
            var s = _database.GetStatistics();
            lblStatistics.Text = $"סטטיסטיקות:\n" +
                $"סה\"כ משחקים: {s.TotalGames}\n" +
                $"ניצחונות שחקן: {s.PlayerWins} ({s.PlayerWinPercentage:F1}%)\n" +
                $"ניצחונות שרת: {s.ServerWins} ({s.ServerWinPercentage:F1}%)\n" +
                $"פסילות זמן: {s.Timeouts}";
            lblStatistics.Location = new Point(85, 485);
        }

        #region Event Handlers

        private void btnLoadGame_Click(object sender, EventArgs e)
        {
            if (cboGames.SelectedItem == null)
            {
                MessageBox.Show("נא לבחור משחק", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _currentGame = (LocalGame)cboGames.SelectedItem;
            _currentMoves = _database.GetGameMoves(_currentGame.LocalGameId);
            _currentMoveIndex = -1;

            lstMoves.Items.Clear();
            int n = 1;
            foreach (var m in _currentMoves)
            {
                string p = m.IsPlayerMove ? "שחקן" : "שרת";
                string capture = m.IsCapture ? " [אכילה]" : "";
                string backward = m.IsBackwardMove ? " [אחורה]" : "";
                lstMoves.Items.Add($"{n++}. {p}: ({m.FromRow},{m.FromCol})→({m.ToRow},{m.ToCol}){capture}{backward}");
            }

            string result = _currentGame.Result switch
            {
                "PlayerWin" or "Win" => "ניצחון שחקן",
                "ServerWin" or "Loss" => "ניצחון שרת",
                "Timeout" => "פסילת זמן",
                _ => _currentGame.Result ?? "לא ידוע"
            };

            // Display all players if multi-player game
            string playersDisplay = _currentGame.TotalPlayers > 1
                ? $"שחקנים ({_currentGame.TotalPlayers}): {_currentGame.DisplayPlayerNames}"
                : $"שחקן: {_currentGame.PlayerName}";

            lblGameInfo.Text = $"פרטי משחק:\n" +
                $"{playersDisplay}\n" +
                $"התחלה: {_currentGame.StartTime:dd/MM/yyyy HH:mm}\n" +
                $"תוצאה: {result}\n" +
                $"מגבלת זמן: {_currentGame.TimeLimitSeconds} שניות";
            lblGameInfo.Location = new Point(180, 600);

            trackProgress.Minimum = 0;
            trackProgress.Maximum = Math.Max(1, _currentMoves.Count);
            trackProgress.Value = 0;
            trackProgress.Enabled = true;

            ResetBoard();
            pnlBoard.Invalidate();
            EnableButtons(true);
            lblMoveInfo.Text = "מהלך נוכחי:\nעמדת פתיחה";
        }

        private void btnFirst_Click(object sender, EventArgs e) => GoToMove(-1);
        private void btnPrev_Click(object sender, EventArgs e) => GoToMove(_currentMoveIndex - 1);
        private void btnNext_Click(object sender, EventArgs e) => GoToMove(_currentMoveIndex + 1);
        private void btnLast_Click(object sender, EventArgs e) => GoToMove(_currentMoves.Count - 1);

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (_currentMoves.Count == 0) return;
            isPlaying = true;
            playTimer.Start();
            btnPlay.Enabled = false;
            btnStop.Enabled = true;
            btnFirst.Enabled = btnPrev.Enabled = btnNext.Enabled = btnLast.Enabled = false;
            trackProgress.Enabled = lstMoves.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            isPlaying = false;
            playTimer.Stop();
            btnStop.Enabled = false;
            trackProgress.Enabled = lstMoves.Enabled = true;
            UpdateButtons();
        }

        private void lstMoves_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isPlaying && lstMoves.SelectedIndex >= 0)
                GoToMove(lstMoves.SelectedIndex);
        }

        private void trackProgress_ValueChanged(object sender, EventArgs e)
        {
            if (!isPlaying && _currentMoves.Count > 0 && trackProgress.Value != _currentMoveIndex + 1)
                GoToMove(trackProgress.Value - 1);
        }

        private void PlayTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentMoves.Count > 0 && _currentMoveIndex < _currentMoves.Count - 1)
                GoToMove(_currentMoveIndex + 1);
            else
                btnStop_Click(this, e);
        }

        #endregion

        #region Navigation

        private void GoToMove(int idx)
        {
            if (_currentMoves.Count == 0) return;

            ResetBoard();
            int target = Math.Max(-1, Math.Min(idx, _currentMoves.Count - 1));

            for (int i = 0; i <= target; i++)
            {
                var m = _currentMoves[i];
                int piece = board[m.FromRow, m.FromCol];
                board[m.FromRow, m.FromCol] = 0;
                board[m.ToRow, m.ToCol] = piece;
                if (m.IsCapture)
                    board[(m.FromRow + m.ToRow) / 2, (m.FromCol + m.ToCol) / 2] = 0;
            }

            _currentMoveIndex = target;
            if (!isPlaying) trackProgress.Value = target + 1;

            if (target >= 0)
            {
                lstMoves.SelectedIndex = target;
                var m = _currentMoves[target];
                string player = m.IsPlayerMove ? "שחקן" : "שרת";
                lblMoveInfo.Text = $"מהלך {target + 1}/{_currentMoves.Count}:\n" +
                    $"{player}: ({m.FromRow},{m.FromCol}) → ({m.ToRow},{m.ToCol})" +
                    $"{(m.IsCapture ? "\n[אכילה]" : "")}" +
                    $"{(m.IsBackwardMove ? "\n[צעד אחורה]" : "")}";
                lblMoveInfo.Location = new Point(280, 485);
            }
            else
            {
                lstMoves.SelectedIndex = -1;
                lblMoveInfo.Text = "מהלך נוכחי:\nעמדת פתיחה";
                lblMoveInfo.Location = new Point(317, 485);
            }

            UpdateButtons();
            pnlBoard.Invalidate();
        }

        private void UpdateButtons()
        {
            bool has = _currentMoves.Count > 0;
            btnFirst.Enabled = btnPrev.Enabled = has && _currentMoveIndex > -1;
            btnNext.Enabled = btnLast.Enabled = btnPlay.Enabled = has && _currentMoveIndex < _currentMoves.Count - 1;
        }

        private void EnableButtons(bool e)
        {
            btnFirst.Enabled = btnPrev.Enabled = btnNext.Enabled = btnLast.Enabled = btnPlay.Enabled = e;
            btnStop.Enabled = false;
        }

        #endregion

        #region Paint

        private void PnlBoard_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var textBrush = new SolidBrush(COLOR_TITLE))
            {
                for (int c = 0; c < BOARD_COLS; c++)
                    g.DrawString(((char)('A' + c)).ToString(), font, textBrush,
                        BOARD_OFFSET_X + c * CELL_SIZE + CELL_SIZE / 2 - 5, 3);
                for (int r = 0; r < BOARD_ROWS; r++)
                    g.DrawString((r + 1).ToString(), font, textBrush,
                        5, BOARD_OFFSET_Y + r * CELL_SIZE + CELL_SIZE / 2 - 8);
            }

            for (int r = 0; r < BOARD_ROWS; r++)
            {
                for (int c = 0; c < BOARD_COLS; c++)
                {
                    var cell = boardCells[r, c];

                    using (var cellBrush = new SolidBrush((r + c) % 2 == 0 ? COLOR_CELL_DARK : COLOR_CELL_LIGHT))
                        g.FillRectangle(cellBrush, cell);
                    using (var borderPen = new Pen(COLOR_CELL_BORDER, 1))
                        g.DrawRectangle(borderPen, cell);

                    if (board[r, c] != 0)
                    {
                        int padding = 6;
                        var rect = new Rectangle(cell.X + padding, cell.Y + padding,
                            cell.Width - padding * 2, cell.Height - padding * 2);
                        Color col = board[r, c] == 1 ? COLOR_PLAYER_PIECE : COLOR_SERVER_PIECE;

                        using (var path = new GraphicsPath())
                        {
                            path.AddEllipse(rect);
                            using (var gradBrush = new PathGradientBrush(path))
                            {
                                gradBrush.CenterColor = Color.FromArgb(200, COLOR_PIECE_HIGHLIGHT);
                                gradBrush.SurroundColors = new[] { col };
                                gradBrush.CenterPoint = new PointF(rect.X + rect.Width / 3f, rect.Y + rect.Height / 3f);
                                g.FillEllipse(gradBrush, rect);
                            }
                        }
                        using (var piecePen = new Pen(COLOR_PIECE_BORDER, 2))
                            g.DrawEllipse(piecePen, rect);
                    }
                }
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            playTimer.Stop();
            playTimer.Dispose();
            base.OnFormClosing(e);
        }
    }
}