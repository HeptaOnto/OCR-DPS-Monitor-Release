namespace OCR_DPS_Monitor
{
    partial class ManualCharacterLoaderForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox _characterNameTextBox;
        private System.Windows.Forms.Button _loadButton;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.ListBox _leftListBox;
        private System.Windows.Forms.ListBox _rightListBox;
        private System.Windows.Forms.Button _saveButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.ContextMenuStrip _contextMenu;
        private System.Windows.Forms.ToolStripMenuItem deleteMenuItem;
        private System.Windows.Forms.Label nameLabel;

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
            nameLabel = new Label();
            _characterNameTextBox = new TextBox();
            _loadButton = new Button();
            _statusLabel = new Label();
            _leftListBox = new ListBox();
            _rightListBox = new ListBox();
            _saveButton = new Button();
            _cancelButton = new Button();
            _contextMenu = new ContextMenuStrip(components);
            deleteMenuItem = new ToolStripMenuItem();
            _contextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new Point(20, 22);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new Size(98, 15);
            nameLabel.TabIndex = 0;
            nameLabel.Text = "Имя персонажа:";
            // 
            // _characterNameTextBox
            // 
            _characterNameTextBox.Location = new Point(130, 20);
            _characterNameTextBox.Name = "_characterNameTextBox";
            _characterNameTextBox.Size = new Size(200, 23);
            _characterNameTextBox.TabIndex = 1;
            // 
            // _loadButton
            // 
            _loadButton.Location = new Point(340, 19);
            _loadButton.Name = "_loadButton";
            _loadButton.Size = new Size(140, 25);
            _loadButton.TabIndex = 2;
            _loadButton.Text = "Загрузить наследие";
            _loadButton.UseVisualStyleBackColor = true;
            // 
            // _statusLabel
            // 
            _statusLabel.AutoSize = true;
            _statusLabel.ForeColor = Color.Gray;
            _statusLabel.Location = new Point(20, 50);
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Size = new Size(96, 15);
            _statusLabel.TabIndex = 3;
            _statusLabel.Text = "Готов к загрузке";
            // 
            // _leftListBox
            // 
            _leftListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _leftListBox.FormattingEnabled = true;
            _leftListBox.ItemHeight = 15;
            _leftListBox.Location = new Point(20, 80);
            _leftListBox.Name = "_leftListBox";
            _leftListBox.SelectionMode = SelectionMode.MultiExtended;
            _leftListBox.Size = new Size(270, 304);
            _leftListBox.TabIndex = 4;
            // 
            // _rightListBox
            // 
            _rightListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _rightListBox.FormattingEnabled = true;
            _rightListBox.ItemHeight = 15;
            _rightListBox.Location = new Point(300, 80);
            _rightListBox.Name = "_rightListBox";
            _rightListBox.SelectionMode = SelectionMode.MultiExtended;
            _rightListBox.Size = new Size(270, 304);
            _rightListBox.TabIndex = 5;
            // 
            // _saveButton
            // 
            _saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _saveButton.Location = new Point(150, 390);
            _saveButton.Name = "_saveButton";
            _saveButton.Size = new Size(140, 30);
            _saveButton.TabIndex = 6;
            _saveButton.Text = "Сохранить и выйти";
            _saveButton.UseVisualStyleBackColor = true;
            // 
            // _cancelButton
            // 
            _cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _cancelButton.Location = new Point(300, 390);
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Size = new Size(140, 30);
            _cancelButton.TabIndex = 7;
            _cancelButton.Text = "Отмена (ESC)";
            _cancelButton.UseVisualStyleBackColor = true;
            // 
            // _contextMenu
            // 
            _contextMenu.Items.AddRange(new ToolStripItem[] { deleteMenuItem });
            _contextMenu.Name = "_contextMenu";
            _contextMenu.Size = new Size(119, 26);
            // 
            // deleteMenuItem
            // 
            deleteMenuItem.Name = "deleteMenuItem";
            deleteMenuItem.Size = new Size(118, 22);
            deleteMenuItem.Text = "Удалить";
            // 
            // ManualCharacterLoaderForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 450);
            Controls.Add(_cancelButton);
            Controls.Add(_saveButton);
            Controls.Add(_rightListBox);
            Controls.Add(_leftListBox);
            Controls.Add(_statusLabel);
            Controls.Add(_loadButton);
            Controls.Add(_characterNameTextBox);
            Controls.Add(nameLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ManualCharacterLoaderForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Загрузчик персонажей наследия";
            TopMost = true;
            _contextMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}