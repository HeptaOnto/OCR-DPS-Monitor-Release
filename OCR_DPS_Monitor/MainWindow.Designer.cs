namespace OCR_DPS_Monitor
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnToggle = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            btnSetRegion = new Button();
            btnSetDps = new Button();
            richTextBoxDebug = new RichTextBox();
            btnSetLegacy = new Button();
            btnCreateLobby = new Button();
            btnRaidParty = new Button();
            SuspendLayout();
            // 
            // btnToggle
            // 
            btnToggle.FlatStyle = FlatStyle.Flat;
            btnToggle.Font = new Font("Segoe UI", 18F);
            btnToggle.Location = new Point(81, 176);
            btnToggle.Name = "btnToggle";
            btnToggle.Size = new Size(204, 52);
            btnToggle.TabIndex = 1;
            btnToggle.Text = "Подключиться";
            btnToggle.UseVisualStyleBackColor = true;
            btnToggle.Click += btnToggle_Click;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            // 
            // btnSetRegion
            // 
            btnSetRegion.FlatStyle = FlatStyle.Flat;
            btnSetRegion.Font = new Font("Segoe UI", 18F);
            btnSetRegion.Location = new Point(-130, 244);
            btnSetRegion.Name = "btnSetRegion";
            btnSetRegion.Size = new Size(204, 42);
            btnSetRegion.TabIndex = 2;
            btnSetRegion.Text = "Окно рейда";
            btnSetRegion.UseVisualStyleBackColor = true;
            btnSetRegion.Visible = false;
            btnSetRegion.Click += btnSetRegion_Click;
            // 
            // btnSetDps
            // 
            btnSetDps.FlatStyle = FlatStyle.Flat;
            btnSetDps.Font = new Font("Segoe UI", 16F);
            btnSetDps.Location = new Point(81, 334);
            btnSetDps.Name = "btnSetDps";
            btnSetDps.Size = new Size(204, 42);
            btnSetDps.TabIndex = 2;
            btnSetDps.Text = "DPS метр";
            btnSetDps.UseVisualStyleBackColor = true;
            btnSetDps.Click += btnSetDps_Click;
            // 
            // richTextBoxDebug
            // 
            richTextBoxDebug.BorderStyle = BorderStyle.None;
            richTextBoxDebug.Location = new Point(81, 0);
            richTextBoxDebug.Name = "richTextBoxDebug";
            richTextBoxDebug.ReadOnly = true;
            richTextBoxDebug.Size = new Size(204, 167);
            richTextBoxDebug.TabIndex = 3;
            richTextBoxDebug.Text = "";
            // 
            // btnSetLegacy
            // 
            btnSetLegacy.FlatStyle = FlatStyle.Flat;
            btnSetLegacy.Font = new Font("Segoe UI", 16F);
            btnSetLegacy.Location = new Point(81, 378);
            btnSetLegacy.Name = "btnSetLegacy";
            btnSetLegacy.Size = new Size(204, 42);
            btnSetLegacy.TabIndex = 4;
            btnSetLegacy.Text = "Наследие";
            btnSetLegacy.UseVisualStyleBackColor = true;
            btnSetLegacy.Click += btnSetLegacy_Click;
            // 
            // btnCreateLobby
            // 
            btnCreateLobby.FlatStyle = FlatStyle.Flat;
            btnCreateLobby.Font = new Font("Segoe UI", 18F);
            btnCreateLobby.Location = new Point(81, 230);
            btnCreateLobby.Name = "btnCreateLobby";
            btnCreateLobby.Size = new Size(204, 52);
            btnCreateLobby.TabIndex = 5;
            btnCreateLobby.Text = "Создать лобби";
            btnCreateLobby.UseVisualStyleBackColor = true;
            btnCreateLobby.Click += btnCreateLobby_Click;
            // 
            // btnRaidParty
            // 
            btnRaidParty.FlatStyle = FlatStyle.Flat;
            btnRaidParty.Font = new Font("Segoe UI", 16F);
            btnRaidParty.Location = new Point(81, 290);
            btnRaidParty.Name = "btnRaidParty";
            btnRaidParty.Size = new Size(204, 42);
            btnRaidParty.TabIndex = 6;
            btnRaidParty.Text = "Группы рейда";
            btnRaidParty.UseVisualStyleBackColor = true;
            btnRaidParty.Click += btnRaidParty_Click;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(321, 425);
            Controls.Add(btnRaidParty);
            Controls.Add(btnCreateLobby);
            Controls.Add(btnSetLegacy);
            Controls.Add(richTextBoxDebug);
            Controls.Add(btnSetDps);
            Controls.Add(btnSetRegion);
            Controls.Add(btnToggle);
            Name = "MainWindow";
            Text = "OCR DPS Monitor";
            Load += MainWindow_Load;
            ResumeLayout(false);
        }

        #endregion
        private Button btnToggle;
        private System.Windows.Forms.Timer timer1;
        private Button btnSetRegion;
        private Button btnSetDps;
        private RichTextBox richTextBoxDebug;
        private Button btnSetLegacy;
        private Button btnCreateLobby;
        private Button btnRaidParty;
    }
}