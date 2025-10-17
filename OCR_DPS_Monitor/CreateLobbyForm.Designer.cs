using System.Reflection;

namespace OCR_DPS_Monitor
{
    partial class CreateLobbyForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Label labelCharacterName;
        private ComboBox comboBoxCharacterName;
        private GroupBox groupBoxParty;
        private RadioButton radioButtonGroup1;
        private RadioButton radioButtonGroup2;
        private GroupBox groupBoxRole;
        private RadioButton radioButtonDD;
        private RadioButton radioButtonSupport;
        private Label labelLobbyCode;
        private TextBox textBoxLobbyCode;
        private Button buttonAction;
        private Button buttonCancel;


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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateLobbyForm));
            labelCharacterName = new Label();
            comboBoxCharacterName = new ComboBox();
            groupBoxParty = new GroupBox();
            radioButtonGroup1 = new RadioButton();
            radioButtonGroup2 = new RadioButton();
            groupBoxRole = new GroupBox();
            radioButtonDD = new RadioButton();
            radioButtonSupport = new RadioButton();
            labelLobbyCode = new Label();
            textBoxLobbyCode = new TextBox();
            buttonAction = new Button();
            buttonCancel = new Button();
            lblLobbyInfo = new Label();
            groupBoxParty.SuspendLayout();
            groupBoxRole.SuspendLayout();
            SuspendLayout();
            // 
            // labelCharacterName
            // 
            labelCharacterName.Location = new Point(12, 15);
            labelCharacterName.Name = "labelCharacterName";
            labelCharacterName.Size = new Size(100, 20);
            labelCharacterName.TabIndex = 0;
            labelCharacterName.Text = "Имя персонажа:";
            // 
            // comboBoxCharacterName
            // 
            comboBoxCharacterName.Location = new Point(120, 12);
            comboBoxCharacterName.Name = "comboBoxCharacterName";
            comboBoxCharacterName.Size = new Size(150, 23);
            comboBoxCharacterName.TabIndex = 1;
            // 
            // groupBoxParty
            // 
            groupBoxParty.Controls.Add(radioButtonGroup1);
            groupBoxParty.Controls.Add(radioButtonGroup2);
            groupBoxParty.Location = new Point(12, 45);
            groupBoxParty.Name = "groupBoxParty";
            groupBoxParty.Size = new Size(260, 60);
            groupBoxParty.TabIndex = 2;
            groupBoxParty.TabStop = false;
            groupBoxParty.Text = "Выберите группу:";
            // 
            // radioButtonGroup1
            // 
            radioButtonGroup1.Location = new Point(10, 25);
            radioButtonGroup1.Name = "radioButtonGroup1";
            radioButtonGroup1.Size = new Size(84, 20);
            radioButtonGroup1.TabIndex = 0;
            radioButtonGroup1.Text = "Группа 1";
            // 
            // radioButtonGroup2
            // 
            radioButtonGroup2.Location = new Point(100, 25);
            radioButtonGroup2.Name = "radioButtonGroup2";
            radioButtonGroup2.Size = new Size(81, 20);
            radioButtonGroup2.TabIndex = 1;
            radioButtonGroup2.Text = "Группа 2";
            // 
            // groupBoxRole
            // 
            groupBoxRole.Controls.Add(radioButtonDD);
            groupBoxRole.Controls.Add(radioButtonSupport);
            groupBoxRole.Location = new Point(12, 115);
            groupBoxRole.Name = "groupBoxRole";
            groupBoxRole.Size = new Size(260, 60);
            groupBoxRole.TabIndex = 3;
            groupBoxRole.TabStop = false;
            groupBoxRole.Text = "Ваша роль:";
            // 
            // radioButtonDD
            // 
            radioButtonDD.Location = new Point(10, 25);
            radioButtonDD.Name = "radioButtonDD";
            radioButtonDD.Size = new Size(50, 20);
            radioButtonDD.TabIndex = 0;
            radioButtonDD.Text = "ДД";
            // 
            // radioButtonSupport
            // 
            radioButtonSupport.Location = new Point(100, 25);
            radioButtonSupport.Name = "radioButtonSupport";
            radioButtonSupport.Size = new Size(81, 20);
            radioButtonSupport.TabIndex = 1;
            radioButtonSupport.Text = "Саппорт";
            // 
            // labelLobbyCode
            // 
            labelLobbyCode.Location = new Point(12, 185);
            labelLobbyCode.Name = "labelLobbyCode";
            labelLobbyCode.Size = new Size(100, 20);
            labelLobbyCode.TabIndex = 4;
            labelLobbyCode.Text = "Код лобби:";
            // 
            // textBoxLobbyCode
            // 
            textBoxLobbyCode.Location = new Point(120, 182);
            textBoxLobbyCode.MaxLength = 6;
            textBoxLobbyCode.Name = "textBoxLobbyCode";
            textBoxLobbyCode.Size = new Size(150, 23);
            textBoxLobbyCode.TabIndex = 5;
            textBoxLobbyCode.TextChanged += textBoxLobbyCode_TextChanged;
            textBoxLobbyCode.KeyDown += textBoxLobbyCode_KeyDown_1;
            textBoxLobbyCode.KeyPress += textBoxLobbyCode_KeyPress;
            // 
            // buttonAction
            // 
            buttonAction.Location = new Point(50, 220);
            buttonAction.Name = "buttonAction";
            buttonAction.Size = new Size(100, 30);
            buttonAction.TabIndex = 6;
            buttonAction.UseVisualStyleBackColor = true;
            buttonAction.Click += buttonAction_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(160, 220);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(100, 30);
            buttonCancel.TabIndex = 7;
            buttonCancel.Text = "Отмена (ESC)";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // lblLobbyInfo
            // 
            lblLobbyInfo.AutoSize = true;
            lblLobbyInfo.Location = new Point(13, 190);
            lblLobbyInfo.Name = "lblLobbyInfo";
            lblLobbyInfo.Size = new Size(259, 15);
            lblLobbyInfo.TabIndex = 8;
            lblLobbyInfo.Text = "Код лобби будет скопирован в буфер обмена";
            // 
            // CreateLobbyForm
            // 
            ClientSize = new Size(284, 261);
            Controls.Add(lblLobbyInfo);
            Controls.Add(labelCharacterName);
            Controls.Add(comboBoxCharacterName);
            Controls.Add(groupBoxParty);
            Controls.Add(groupBoxRole);
            Controls.Add(labelLobbyCode);
            Controls.Add(textBoxLobbyCode);
            Controls.Add(buttonAction);
            Controls.Add(buttonCancel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "CreateLobbyForm";
            groupBoxParty.ResumeLayout(false);
            groupBoxRole.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblLobbyInfo;
    }
}