using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace OCR_DPS_Monitor
{

    public partial class CreateLobbyForm : Form
    {
        private LobbyFormMode _mode;
        private List<string> _characterNames;
        public LobbyFormResult Result { get; private set; }

        public CreateLobbyForm(List<string> characterNames, LobbyFormMode mode)
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F); // 96 DPI - стандарт
            this.Font = new Font("Segoe UI", 9F); 
            try
            {
                InitializeComponent();
                SetupScalingAndLayout();
                _characterNames = characterNames;
                _mode = mode;

                Result = new LobbyFormResult { Success = false };
                InitializeForm();
                
                SetupControls();

                Result = new LobbyFormResult { Success = false };
                this.FormClosing += CreateLobbyForm_FormClosing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
        private void CreateLobbyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Гарантируем, что DialogResult установлен при закрытии
            if (this.DialogResult == DialogResult.None)
            {
                this.DialogResult = DialogResult.Cancel;
            }

            // Очищаем обработчики событий
            this.KeyDown -= CreateLobbyForm_KeyDown;
            this.FormClosing -= CreateLobbyForm_FormClosing;
        }

        private void InitializeForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.TopMost = true;
            this.Text = _mode == LobbyFormMode.Create ? "Создание лобби" : "Подключение к лобби";
            this.KeyPreview = true;
            this.KeyDown += CreateLobbyForm_KeyDown;
        }


        private void SetupScalingAndLayout()
        {
            // Получаем коэффициент масштабирования
            float scaleFactor = this.CurrentAutoScaleDimensions.Width / 96F;

            // Функция для масштабирования
            int Scale(int value) => (int)(value * scaleFactor);

            // Обновляем размер формы
            this.ClientSize = new Size(Scale(300), Scale(280));


            // РАСЧЕТ ШИРИНЫ КОНТРОЛОВ ОТНОСИТЕЛЬНО ФОРМЫ
            int formWidth = this.ClientSize.Width;
            int margin = Scale(15);
            int labelWidth = Scale(100);
            int controlWidth = formWidth - margin * 2 - labelWidth - Scale(10);
            // Ограничиваем минимальную и максимальную ширину
            controlWidth = Math.Max(Scale(130), controlWidth); // минимум 130
            controlWidth = Math.Min(Scale(300), controlWidth); // максимум 300


            // Сбрасываем все расположения и размеры
            labelCharacterName.AutoSize = true;
            labelCharacterName.Location = new Point(Scale(15), Scale(15));

            comboBoxCharacterName.Location = new Point(Scale(120), Scale(12));
            comboBoxCharacterName.Size = new Size(controlWidth, Scale(23)); //Scale(130)
            comboBoxCharacterName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            groupBoxParty.Location = new Point(Scale(15), Scale(45));
            groupBoxParty.Size = new Size(Scale(260), Scale(60));
            groupBoxParty.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            radioButtonGroup1.AutoSize = true;
            radioButtonGroup1.Location = new Point(Scale(15), Scale(25));

            radioButtonGroup2.AutoSize = true;
            radioButtonGroup2.Location = new Point(Scale(115), Scale(25));

            groupBoxRole.Location = new Point(Scale(15), Scale(115));
            groupBoxRole.Size = new Size(Scale(260), Scale(60));
            groupBoxRole.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            radioButtonDD.AutoSize = true;
            radioButtonDD.Location = new Point(Scale(15), Scale(25));

            radioButtonSupport.AutoSize = true;
            radioButtonSupport.Location = new Point(Scale(115), Scale(25));

            labelLobbyCode.AutoSize = true;
            labelLobbyCode.Location = new Point(Scale(15), Scale(185));

            textBoxLobbyCode.Location = new Point(Scale(120), Scale(182));
            textBoxLobbyCode.Size = new Size(controlWidth, Scale(23)); //130
            textBoxLobbyCode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Настройка кнопок
            buttonAction.Location = new Point(Scale(50), Scale(220));
            buttonAction.Size = new Size(Scale(100), Scale(30));

            buttonCancel.Location = new Point(Scale(160), Scale(220));
            buttonCancel.Size = new Size(Scale(100), Scale(30));

            // Настройка информационной метки
            lblLobbyInfo.AutoSize = false; // Отключаем AutoSize для переноса текста
            lblLobbyInfo.Location = new Point(Scale(15), Scale(180));
            lblLobbyInfo.Size = new Size(this.ClientSize.Width - Scale(30), Scale(30));
            lblLobbyInfo.TextAlign = ContentAlignment.MiddleLeft;
        }



        //private void SetupScalingAndLayout()
        //{
        //    // Настройка AutoSize для предотвращения обрезки текста
        //    labelCharacterName.AutoSize = true;
        //    labelLobbyCode.AutoSize = true;
        //    lblLobbyInfo.AutoSize = true;

        //    radioButtonGroup1.AutoSize = true;
        //    radioButtonGroup2.AutoSize = true;
        //    radioButtonDD.AutoSize = true;
        //    radioButtonSupport.AutoSize = true;

        //    // Настройка Anchor для адаптивности
        //    comboBoxCharacterName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        //    textBoxLobbyCode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        //    groupBoxParty.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        //    groupBoxRole.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        //    buttonAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        //    buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        //    this.MinimumSize = new Size(320, 300);
        //}

        private void SetupControls()
        {
            // Настройка выпадающего списка с автодополнением
            comboBoxCharacterName.DropDownStyle = ComboBoxStyle.DropDown;
            comboBoxCharacterName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBoxCharacterName.AutoCompleteSource = AutoCompleteSource.ListItems;
            comboBoxCharacterName.DataSource = _characterNames;

            // Настройка радиокнопок групп
            radioButtonGroup1.Checked = true;

            // Настройка радиокнопок ролей
            radioButtonDD.Checked = true;

            // Показываем/скрываем поле для кода лобби
            labelLobbyCode.Visible = _mode == LobbyFormMode.Join;
            textBoxLobbyCode.Visible = _mode == LobbyFormMode.Join;
            lblLobbyInfo.Visible = !(_mode == LobbyFormMode.Join);

            // Настраиваем текст кнопки действия
            buttonAction.Text = _mode == LobbyFormMode.Create ? "Создать лобби" : "Подключиться";
        }

        private void buttonAction_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            // Заполняем результат
            Result.Success = true;
            Result.CharacterName = comboBoxCharacterName.SelectedItem.ToString();
            Result.PartyNumber = radioButtonGroup1.Checked ? 1 : 2;
            Result.ClassType = radioButtonDD.Checked ? 1 : 0; // 1 - ДД, 0 - Саппорт

            if (_mode == LobbyFormMode.Join)
            {
                Result.LobbyCode = textBoxLobbyCode.Text;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }


        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(comboBoxCharacterName.Text))
            {
                MessageBox.Show("Выберите имя персонажа", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Проверка, что введенное значение есть в списке
            if (!_characterNames.Contains(comboBoxCharacterName.Text))
            {
                MessageBox.Show("Выберите имя персонажа из списка", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBoxCharacterName.Focus();
                comboBoxCharacterName.SelectAll();
                return false;
            }

            if (_mode == LobbyFormMode.Join)
            {
                if (string.IsNullOrEmpty(textBoxLobbyCode.Text) ||
                    textBoxLobbyCode.Text.Length != 6 ||
                    !int.TryParse(textBoxLobbyCode.Text, out _))
                {
                    MessageBox.Show("Введите корректный 6-значный код лобби", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        // Обработчик для ограничения ввода в поле кода лобби
        private void textBoxLobbyCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

            TextBox textBox = (TextBox)sender;
            int currentLength = textBox.Text.Length;
            int selectedLength = textBox.SelectionLength;

            // Если есть выделенный текст, он будет заменен, поэтому вычитаем его длину
            int projectedLength = currentLength - selectedLength + 1;

            if (projectedLength > 6)
            {
                e.Handled = true;
            }
        }

        // Ограничение длины кода лобби
        private void textBoxLobbyCode_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLobbyCode.Text.Length > 6)
            {
                textBoxLobbyCode.Text = textBoxLobbyCode.Text.Substring(0, 6);
                textBoxLobbyCode.SelectionStart = 6;
            }
        }
        private void CreateLobbyForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                buttonCancel.PerformClick();
                e.Handled = true;
            }
        }

        private void textBoxLobbyCode_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Отключает звуковой сигнал
                buttonAction.PerformClick();
            }
        }
    }

    public enum LobbyFormMode
    {
        Create,
        Join
    }

    public class LobbyFormResult
    {
        public bool Success { get; set; }
        public string CharacterName { get; set; }
        public int PartyNumber { get; set; }
        public int ClassType { get; set; }
        public string LobbyCode { get; set; } // Только для Join
    }
}
