namespace CheckersClient
{
    partial class ReplayForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblTitle = new Label();
            lblSelect = new Label();
            cboGames = new ComboBox();
            btnLoadGame = new Button();
            pnlBoard = new Panel();
            lblMoves = new Label();
            lstMoves = new ListBox();
            lblGameInfo = new Label();
            trackProgress = new TrackBar();
            lblMoveInfo = new Label();
            lblStatistics = new Label();
            btnFirst = new Button();
            btnPrev = new Button();
            btnPlay = new Button();
            btnStop = new Button();
            btnNext = new Button();
            btnLast = new Button();
            playTimer = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1 = new TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)trackProgress).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(311, 22);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(142, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "צפייה בחזרות משחק";
            // 
            // lblSelect
            // 
            lblSelect.AutoSize = true;
            lblSelect.Location = new Point(659, 70);
            lblSelect.Name = "lblSelect";
            lblSelect.RightToLeft = RightToLeft.Yes;
            lblSelect.Size = new Size(85, 20);
            lblSelect.TabIndex = 1;
            lblSelect.Text = "בחר משחק:";
            // 
            // cboGames
            // 
            cboGames.DropDownStyle = ComboBoxStyle.DropDownList;
            cboGames.FormattingEnabled = true;
            cboGames.Location = new Point(148, 70);
            cboGames.Name = "cboGames";
            cboGames.RightToLeft = RightToLeft.Yes;
            cboGames.Size = new Size(505, 28);
            cboGames.TabIndex = 2;
            // 
            // btnLoadGame
            // 
            btnLoadGame.Location = new Point(48, 70);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.Size = new Size(94, 29);
            btnLoadGame.TabIndex = 3;
            btnLoadGame.Text = "טען משחק";
            btnLoadGame.UseVisualStyleBackColor = true;
            btnLoadGame.Click += btnLoadGame_Click;
            // 
            // pnlBoard
            // 
            pnlBoard.Location = new Point(464, 118);
            pnlBoard.Name = "pnlBoard";
            pnlBoard.Size = new Size(280, 560);
            pnlBoard.TabIndex = 4;
            pnlBoard.Paint += PnlBoard_Paint;
            // 
            // lblMoves
            // 
            lblMoves.AutoSize = true;
            lblMoves.Location = new Point(182, 132);
            lblMoves.Name = "lblMoves";
            lblMoves.RightToLeft = RightToLeft.Yes;
            lblMoves.Size = new Size(62, 20);
            lblMoves.TabIndex = 5;
            lblMoves.Text = "מהלכים:";
            // 
            // lstMoves
            // 
            lstMoves.FormattingEnabled = true;
            lstMoves.Location = new Point(48, 163);
            lstMoves.Name = "lstMoves";
            lstMoves.RightToLeft = RightToLeft.Yes;
            lstMoves.Size = new Size(359, 304);
            lstMoves.TabIndex = 6;
            lstMoves.SelectedIndexChanged += lstMoves_SelectedIndexChanged;
            // 
            // lblGameInfo
            // 
            lblGameInfo.AutoSize = true;
            lblGameInfo.Location = new Point(277, 594);
            lblGameInfo.Name = "lblGameInfo";
            lblGameInfo.RightToLeft = RightToLeft.Yes;
            lblGameInfo.Size = new Size(130, 20);
            lblGameInfo.TabIndex = 7;
            lblGameInfo.Text = "בחר משחק להצגה";
            // 
            // trackProgress
            // 
            trackProgress.Location = new Point(48, 740);
            trackProgress.Name = "trackProgress";
            trackProgress.Size = new Size(696, 56);
            trackProgress.TabIndex = 8;
            trackProgress.ValueChanged += trackProgress_ValueChanged;
            // 
            // lblMoveInfo
            // 
            lblMoveInfo.AutoSize = true;
            lblMoveInfo.Location = new Point(317, 485);
            lblMoveInfo.Name = "lblMoveInfo";
            lblMoveInfo.RightToLeft = RightToLeft.Yes;
            lblMoveInfo.Size = new Size(90, 20);
            lblMoveInfo.TabIndex = 9;
            lblMoveInfo.Text = "מהלך: -- / --";
            // 
            // lblStatistics
            // 
            lblStatistics.AutoSize = true;
            lblStatistics.Location = new Point(85, 485);
            lblStatistics.Name = "lblStatistics";
            lblStatistics.RightToLeft = RightToLeft.Yes;
            lblStatistics.Size = new Size(92, 20);
            lblStatistics.TabIndex = 10;
            lblStatistics.Text = "סטטיסטיקות";
            // 
            // btnFirst
            // 
            btnFirst.Location = new Point(13, 13);
            btnFirst.Margin = new Padding(10);
            btnFirst.Name = "btnFirst";
            btnFirst.Padding = new Padding(3);
            btnFirst.Size = new Size(94, 33);
            btnFirst.TabIndex = 11;
            btnFirst.Text = "התחלה";
            btnFirst.UseVisualStyleBackColor = true;
            btnFirst.Click += btnFirst_Click;
            // 
            // btnPrev
            // 
            btnPrev.Location = new Point(128, 13);
            btnPrev.Margin = new Padding(10);
            btnPrev.Name = "btnPrev";
            btnPrev.Padding = new Padding(3);
            btnPrev.Size = new Size(94, 33);
            btnPrev.TabIndex = 12;
            btnPrev.Text = "קודם";
            btnPrev.UseVisualStyleBackColor = true;
            btnPrev.Click += btnPrev_Click;
            // 
            // btnPlay
            // 
            btnPlay.Location = new Point(243, 13);
            btnPlay.Margin = new Padding(10);
            btnPlay.Name = "btnPlay";
            btnPlay.Padding = new Padding(3);
            btnPlay.Size = new Size(94, 33);
            btnPlay.TabIndex = 13;
            btnPlay.Text = "נגן";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(358, 13);
            btnStop.Margin = new Padding(10);
            btnStop.Name = "btnStop";
            btnStop.Padding = new Padding(3);
            btnStop.Size = new Size(94, 33);
            btnStop.TabIndex = 14;
            btnStop.Text = "עצור";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnNext
            // 
            btnNext.Location = new Point(473, 13);
            btnNext.Margin = new Padding(10);
            btnNext.Name = "btnNext";
            btnNext.Padding = new Padding(3);
            btnNext.Size = new Size(94, 33);
            btnNext.TabIndex = 15;
            btnNext.Text = "הבא";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // btnLast
            // 
            btnLast.Location = new Point(588, 13);
            btnLast.Margin = new Padding(10);
            btnLast.Name = "btnLast";
            btnLast.Padding = new Padding(3);
            btnLast.Size = new Size(94, 33);
            btnLast.TabIndex = 16;
            btnLast.Text = "סיום";
            btnLast.UseVisualStyleBackColor = true;
            btnLast.Click += btnLast_Click;
            // 
            // playTimer
            // 
            playTimer.Interval = 1000;
            playTimer.Tick += PlayTimer_Tick;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.None;
            tableLayoutPanel1.ColumnCount = 6;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel1.Controls.Add(btnFirst, 0, 0);
            tableLayoutPanel1.Controls.Add(btnLast, 5, 0);
            tableLayoutPanel1.Controls.Add(btnPrev, 1, 0);
            tableLayoutPanel1.Controls.Add(btnNext, 4, 0);
            tableLayoutPanel1.Controls.Add(btnPlay, 2, 0);
            tableLayoutPanel1.Controls.Add(btnStop, 3, 0);
            tableLayoutPanel1.Location = new Point(48, 802);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(3);
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(696, 59);
            tableLayoutPanel1.TabIndex = 17;
            // 
            // ReplayForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 887);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(lblStatistics);
            Controls.Add(lblMoveInfo);
            Controls.Add(trackProgress);
            Controls.Add(lblGameInfo);
            Controls.Add(lstMoves);
            Controls.Add(lblMoves);
            Controls.Add(pnlBoard);
            Controls.Add(btnLoadGame);
            Controls.Add(cboGames);
            Controls.Add(lblSelect);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "ReplayForm";
            Text = "ReplayForm";
            ((System.ComponentModel.ISupportInitialize)trackProgress).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblSelect;
        private ComboBox cboGames;
        private Button btnLoadGame;
        private Panel pnlBoard;
        private Label lblMoves;
        private ListBox lstMoves;
        private Label lblGameInfo;
        private TrackBar trackProgress;
        private Label lblMoveInfo;
        private Label lblStatistics;
        private Button btnFirst;
        private Button btnPrev;
        private Button btnPlay;
        private Button btnStop;
        private Button btnNext;
        private Button btnLast;
        private TableLayoutPanel tableLayoutPanel1;
    }
}