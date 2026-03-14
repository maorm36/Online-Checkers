namespace CheckersClient
{
    partial class MainForm
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
            txtIdentificationNumber = new TextBox();
            lblTitle = new Label();
            lblIdPrompt = new Label();
            lblTimePrompt = new Label();
            cmbTimeLimit = new ComboBox();
            btnNewGame = new Button();
            btnReplay = new Button();
            btnClearDrawing = new Button();
            lblPlayerInfo = new Label();
            pnlBoard = new Panel();
            lblTimeRemaining = new Label();
            lblStatus = new Label();
            gameTimer = new System.Windows.Forms.Timer(components);
            animationTimer = new System.Windows.Forms.Timer(components);
            blinkTimer = new System.Windows.Forms.Timer(components);
            cmbDifficulty = new ComboBox();
            lblDifficultyLabel = new Label();
            SuspendLayout();
            // 
            // txtIdentificationNumber
            // 
            txtIdentificationNumber.Location = new Point(566, 367);
            txtIdentificationNumber.MaxLength = 4;
            txtIdentificationNumber.Name = "txtIdentificationNumber";
            txtIdentificationNumber.RightToLeft = RightToLeft.Yes;
            txtIdentificationNumber.Size = new Size(79, 27);
            txtIdentificationNumber.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(330, 21);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(92, 20);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "משחק דמקה";
            // 
            // lblIdPrompt
            // 
            lblIdPrompt.AutoSize = true;
            lblIdPrompt.Location = new Point(651, 370);
            lblIdPrompt.Name = "lblIdPrompt";
            lblIdPrompt.RightToLeft = RightToLeft.Yes;
            lblIdPrompt.Size = new Size(88, 20);
            lblIdPrompt.TabIndex = 2;
            lblIdPrompt.Text = "מספר מזהה:";
            // 
            // lblTimePrompt
            // 
            lblTimePrompt.AutoSize = true;
            lblTimePrompt.Location = new Point(480, 370);
            lblTimePrompt.Name = "lblTimePrompt";
            lblTimePrompt.RightToLeft = RightToLeft.Yes;
            lblTimePrompt.Size = new Size(80, 20);
            lblTimePrompt.TabIndex = 3;
            lblTimePrompt.Text = "מגבלת זמן:";
            // 
            // cmbTimeLimit
            // 
            cmbTimeLimit.Anchor = AnchorStyles.None;
            cmbTimeLimit.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimeLimit.FormattingEnabled = true;
            cmbTimeLimit.Items.AddRange(new object[] { "2 שניות", "5 שניות", "10 שניות", "15 שניות" });
            cmbTimeLimit.Location = new Point(387, 366);
            cmbTimeLimit.Name = "cmbTimeLimit";
            cmbTimeLimit.RightToLeft = RightToLeft.Yes;
            cmbTimeLimit.Size = new Size(87, 28);
            cmbTimeLimit.TabIndex = 4;
            cmbTimeLimit.SelectedIndexChanged += cmbTimeLimit_SelectedIndexChanged;
            // 
            // btnNewGame
            // 
            btnNewGame.Location = new Point(633, 412);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.Size = new Size(106, 29);
            btnNewGame.TabIndex = 5;
            btnNewGame.Text = "התחל משחק";
            btnNewGame.UseVisualStyleBackColor = true;
            btnNewGame.Click += btnNewGame_Click;
            // 
            // btnReplay
            // 
            btnReplay.Location = new Point(507, 412);
            btnReplay.Name = "btnReplay";
            btnReplay.Size = new Size(109, 29);
            btnReplay.TabIndex = 6;
            btnReplay.Text = "צפה בחזרות";
            btnReplay.UseVisualStyleBackColor = true;
            btnReplay.Click += btnReplay_Click;
            // 
            // btnClearDrawing
            // 
            btnClearDrawing.Location = new Point(387, 412);
            btnClearDrawing.Name = "btnClearDrawing";
            btnClearDrawing.Size = new Size(100, 29);
            btnClearDrawing.TabIndex = 7;
            btnClearDrawing.Text = "נקה ציור";
            btnClearDrawing.UseVisualStyleBackColor = true;
            btnClearDrawing.Click += btnClearDrawing_Click;
            // 
            // lblPlayerInfo
            // 
            lblPlayerInfo.AutoSize = true;
            lblPlayerInfo.Location = new Point(497, 76);
            lblPlayerInfo.Name = "lblPlayerInfo";
            lblPlayerInfo.RightToLeft = RightToLeft.Yes;
            lblPlayerInfo.Size = new Size(138, 20);
            lblPlayerInfo.TabIndex = 8;
            lblPlayerInfo.Text = "אנא הזן מספר מזהה";
            // 
            // pnlBoard
            // 
            pnlBoard.Location = new Point(23, 76);
            pnlBoard.Name = "pnlBoard";
            pnlBoard.Size = new Size(290, 462);
            pnlBoard.TabIndex = 9;
            pnlBoard.Paint += PnlBoard_Paint;
            pnlBoard.MouseClick += PnlBoard_MouseClick;
            pnlBoard.MouseDown += PnlBoard_MouseDown;
            pnlBoard.MouseMove += PnlBoard_MouseMove;
            pnlBoard.MouseUp += PnlBoard_MouseUp;
            // 
            // lblTimeRemaining
            // 
            lblTimeRemaining.AutoSize = true;
            lblTimeRemaining.Location = new Point(659, 510);
            lblTimeRemaining.Name = "lblTimeRemaining";
            lblTimeRemaining.RightToLeft = RightToLeft.Yes;
            lblTimeRemaining.Size = new Size(80, 20);
            lblTimeRemaining.TabIndex = 10;
            lblTimeRemaining.Text = "זמן נותר: --";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(387, 510);
            lblStatus.Name = "lblStatus";
            lblStatus.RightToLeft = RightToLeft.Yes;
            lblStatus.Size = new Size(156, 20);
            lblStatus.TabIndex = 11;
            lblStatus.Text = "ממתין להתחלת משחק";
            // 
            // gameTimer
            // 
            gameTimer.Interval = 1000;
            gameTimer.Tick += GameTimer_Tick;
            // 
            // animationTimer
            // 
            animationTimer.Interval = 30;
            animationTimer.Tick += AnimationTimer_Tick;
            // 
            // blinkTimer
            // 
            blinkTimer.Interval = 200;
            blinkTimer.Tick += BlinkTimer_Tick;
            // 
            // cmbDifficulty
            // 
            cmbDifficulty.Anchor = AnchorStyles.None;
            cmbDifficulty.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDifficulty.FormattingEnabled = true;
            cmbDifficulty.Items.AddRange(new object[] { "קל", "בינוני", "קשה" });
            cmbDifficulty.Location = new Point(589, 460);
            cmbDifficulty.Name = "cmbDifficulty";
            cmbDifficulty.RightToLeft = RightToLeft.Yes;
            cmbDifficulty.Size = new Size(69, 28);
            cmbDifficulty.TabIndex = 12;
            cmbDifficulty.SelectedIndexChanged += cmbDifficulty_SelectedIndexChanged;
            // 
            // lblDifficultyLabel
            // 
            lblDifficultyLabel.AutoSize = true;
            lblDifficultyLabel.Location = new Point(664, 463);
            lblDifficultyLabel.Name = "lblDifficultyLabel";
            lblDifficultyLabel.RightToLeft = RightToLeft.Yes;
            lblDifficultyLabel.Size = new Size(75, 20);
            lblDifficultyLabel.TabIndex = 13;
            lblDifficultyLabel.Text = "רמת קושי:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(770, 594);
            Controls.Add(lblDifficultyLabel);
            Controls.Add(cmbDifficulty);
            Controls.Add(lblStatus);
            Controls.Add(lblTimeRemaining);
            Controls.Add(pnlBoard);
            Controls.Add(lblPlayerInfo);
            Controls.Add(btnClearDrawing);
            Controls.Add(btnReplay);
            Controls.Add(btnNewGame);
            Controls.Add(cmbTimeLimit);
            Controls.Add(lblTimePrompt);
            Controls.Add(lblIdPrompt);
            Controls.Add(lblTitle);
            Controls.Add(txtIdentificationNumber);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            Text = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtIdentificationNumber;
        private Label lblTitle;
        private Label lblIdPrompt;
        private Label lblTimePrompt;
        private ComboBox cmbTimeLimit;
        private Button btnNewGame;
        private Button btnReplay;
        private Button btnClearDrawing;
        private Label lblPlayerInfo;
        private Panel pnlBoard;
        private Label lblTimeRemaining;
        private Label lblStatus;
        private System.Windows.Forms.Timer gameTimer;
        private System.Windows.Forms.Timer animationTimer;
        private System.Windows.Forms.Timer blinkTimer;
        private ComboBox cmbDifficulty;
        private Label lblDifficultyLabel;
    }
}