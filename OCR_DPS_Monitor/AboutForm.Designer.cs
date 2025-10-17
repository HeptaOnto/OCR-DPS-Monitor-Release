namespace OCR_DPS_Monitor
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;
        private PictureBox logoPictureBox;
        private PictureBox qrPictureBox;
        private Label githubLabel;
        private LinkLabel boostyLinkLabel;
        private Button closeButton;
        private Panel headerPanel;
        private Label titleLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            headerPanel = new Panel();
            titleLabel = new Label();
            logoPictureBox = new PictureBox();
            qrPictureBox = new PictureBox();
            githubLabel = new Label();
            boostyLinkLabel = new LinkLabel();
            closeButton = new Button();
            label1 = new Label();
            gitPictureBox = new PictureBox();
            gitHubLinkLabel = new LinkLabel();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)qrPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gitPictureBox).BeginInit();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(44, 62, 80);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(20, 20);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(20, 0, 20, 0);
            headerPanel.Size = new Size(460, 54);
            headerPanel.TabIndex = 0;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Dock = DockStyle.Bottom;
            titleLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(20, 4);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(433, 25);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "OCR ДПС монитор для RU версии игры Lost Ark";
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // logoPictureBox
            // 
            logoPictureBox.Image = Properties.Resources.boosty1;
            logoPictureBox.Location = new Point(23, 106);
            logoPictureBox.Name = "logoPictureBox";
            logoPictureBox.Size = new Size(277, 94);
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.TabIndex = 1;
            logoPictureBox.TabStop = false;
            // 
            // qrPictureBox
            // 
            qrPictureBox.BorderStyle = BorderStyle.FixedSingle;
            qrPictureBox.Image = Properties.Resources.donate;
            qrPictureBox.Location = new Point(323, 106);
            qrPictureBox.Name = "qrPictureBox";
            qrPictureBox.Padding = new Padding(5);
            qrPictureBox.Size = new Size(150, 150);
            qrPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            qrPictureBox.TabIndex = 2;
            qrPictureBox.TabStop = false;
            // 
            // githubLabel
            // 
            githubLabel.Font = new Font("Segoe UI", 10F);
            githubLabel.ForeColor = Color.FromArgb(64, 64, 64);
            githubLabel.Location = new Point(107, 297);
            githubLabel.Name = "githubLabel";
            githubLabel.Size = new Size(376, 66);
            githubLabel.TabIndex = 4;
            githubLabel.Text = "Проверить обновления, задать вопросы, внести предложения об улучшении или сообщить об ошибках можно на странице проекта GitHub:";
            githubLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // boostyLinkLabel
            // 
            boostyLinkLabel.ActiveLinkColor = Color.FromArgb(0, 64, 128);
            boostyLinkLabel.AutoSize = true;
            boostyLinkLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            boostyLinkLabel.LinkColor = Color.FromArgb(0, 102, 204);
            boostyLinkLabel.Location = new Point(121, 219);
            boostyLinkLabel.Name = "boostyLinkLabel";
            boostyLinkLabel.Size = new Size(179, 20);
            boostyLinkLabel.TabIndex = 5;
            boostyLinkLabel.TabStop = true;
            boostyLinkLabel.Text = "https://boosty.to/latech";
            boostyLinkLabel.VisitedLinkColor = Color.FromArgb(0, 102, 204);
            boostyLinkLabel.LinkClicked += boostyLinkLabel_LinkClicked;
            // 
            // closeButton
            // 
            closeButton.BackColor = Color.FromArgb(44, 62, 80);
            closeButton.Cursor = Cursors.Hand;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            closeButton.ForeColor = Color.White;
            closeButton.Location = new Point(200, 407);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(100, 35);
            closeButton.TabIndex = 6;
            closeButton.Text = "Закрыть";
            closeButton.UseVisualStyleBackColor = false;
            closeButton.Click += closeButton_Click_1;
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(64, 64, 64);
            label1.Location = new Point(20, 216);
            label1.Name = "label1";
            label1.Size = new Size(104, 26);
            label1.TabIndex = 4;
            label1.Text = "Поддержать:";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // gitPictureBox
            // 
            gitPictureBox.Image = Properties.Resources.github_logo;
            gitPictureBox.Location = new Point(20, 280);
            gitPictureBox.Name = "gitPictureBox";
            gitPictureBox.Padding = new Padding(5);
            gitPictureBox.Size = new Size(81, 118);
            gitPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            gitPictureBox.TabIndex = 2;
            gitPictureBox.TabStop = false;
            // 
            // gitHubLinkLabel
            // 
            gitHubLinkLabel.ActiveLinkColor = Color.FromArgb(0, 64, 128);
            gitHubLinkLabel.AutoSize = true;
            gitHubLinkLabel.Font = new Font("Segoe UI", 10F);
            gitHubLinkLabel.LinkColor = Color.FromArgb(64, 64, 64);
            gitHubLinkLabel.Location = new Point(107, 363);
            gitHubLinkLabel.Name = "gitHubLinkLabel";
            gitHubLinkLabel.Size = new Size(368, 19);
            gitHubLinkLabel.TabIndex = 5;
            gitHubLinkLabel.TabStop = true;
            gitHubLinkLabel.Text = "https://github.com/HeptaOnto/OCR-DPS-Monitor-Release";
            gitHubLinkLabel.VisitedLinkColor = Color.FromArgb(0, 102, 204);
            gitHubLinkLabel.LinkClicked += gitHubLinkLabel_LinkClicked;
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(500, 456);
            Controls.Add(headerPanel);
            Controls.Add(logoPictureBox);
            Controls.Add(gitPictureBox);
            Controls.Add(qrPictureBox);
            Controls.Add(label1);
            Controls.Add(githubLabel);
            Controls.Add(gitHubLinkLabel);
            Controls.Add(boostyLinkLabel);
            Controls.Add(closeButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            Padding = new Padding(20);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "О программе";
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)qrPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)gitPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private Label label1;
        private PictureBox gitPictureBox;
        private LinkLabel gitHubLinkLabel;
    }
}