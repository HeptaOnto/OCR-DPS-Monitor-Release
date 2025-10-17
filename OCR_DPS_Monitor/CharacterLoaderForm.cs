using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OCR_DPS_Monitor
{
    public partial class CharacterLoaderForm : Form
    {
        private readonly HttpClient _httpClient;
        private ListBox _leftListBox;
        private ListBox _rightListBox;
        private ContextMenuStrip _contextMenu;
        private TextBox _characterNameTextBox;
        private Button _loadButton;
        private Button _saveButton;
        private Button _cancelButton;
        private Label _statusLabel;
        private readonly string _configFilePath;

        private List<string> _allCharacters = new List<string>();
        private List<string> _savedCharacters = new List<string>(); // Сохранённые значения
        private bool _isScrolling = false;

        public CharacterLoaderForm()
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F); // 96 DPI - стандарт
            this.Font = new Font("Segoe UI", 9F);

            InitializeComponent();
            _httpClient = new HttpClient();
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "legacy_data.json");
            SetupControls();
            LoadConfig();

            _savedCharacters = new List<string>(_allCharacters); // Сохраняем начальные значения

            this.FormClosing += CharacterLoaderForm_FormClosing;
            this.KeyPreview = true;
        }

        public List<string> GetAllCharacters()
        {
            return new List<string>(_allCharacters); // Возвращаем копию
        }

        private void SetupControls()
        {
            // Настройка формы
            this.Text = "Загрузчик персонажей наследия";
            this.Size = new System.Drawing.Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.TopMost = true;

            // Поле ввода имени персонажа
            var nameLabel = new Label
            {
                Text = "Имя персонажа:",
                Location = new System.Drawing.Point(20, 22),
                Size = new System.Drawing.Size(100, 20)
            };
            this.Controls.Add(nameLabel);

            _characterNameTextBox = new TextBox
            {
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(200, 20)
            };
            _characterNameTextBox.KeyDown += CharacterNameTextBox_KeyDown;
            this.Controls.Add(_characterNameTextBox);

            // Кнопка загрузки
            _loadButton = new Button
            {
                Text = "Загрузить наследие",
                Location = new System.Drawing.Point(340, 19),
                Size = new System.Drawing.Size(140, 25)
            };
            _loadButton.Click += LoadButton_Click;
            this.Controls.Add(_loadButton);

            // Label для статуса
            _statusLabel = new Label
            {
                Text = "Готов к загрузке",
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(300, 20),
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(_statusLabel);

            // Левый ListBox
            _leftListBox = new ListBox
            {
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(270, 300),
                SelectionMode = SelectionMode.MultiExtended,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            _leftListBox.KeyDown += ListBox_KeyDown;
            _leftListBox.MouseDown += ListBox_MouseDown;
            this.Controls.Add(_leftListBox);

            // Правый ListBox
            _rightListBox = new ListBox
            {
                Location = new System.Drawing.Point(300, 80),
                Size = new System.Drawing.Size(270, 300),
                SelectionMode = SelectionMode.MultiExtended,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            _rightListBox.KeyDown += ListBox_KeyDown;
            _rightListBox.MouseDown += ListBox_MouseDown;
            this.Controls.Add(_rightListBox);

            _contextMenu = new ContextMenuStrip();
            var deleteMenuItem = new ToolStripMenuItem("Удалить");
            deleteMenuItem.Click += DeleteMenuItem_Click;
            _contextMenu.Items.Add(deleteMenuItem);

            // Кнопка Сохранить и выйти
            _saveButton = new Button
            {
                Text = "Сохранить и выйти",
                Location = new System.Drawing.Point(150, 390),
                Size = new System.Drawing.Size(140, 30)
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Кнопка Отмена
            _cancelButton = new Button
            {
                Text = "Отмена (ESC)",
                Location = new System.Drawing.Point(300, 390),
                Size = new System.Drawing.Size(140, 30)
            };
            _cancelButton.Click += CancelButton_Click;
            this.Controls.Add(_cancelButton);

            // Обработка клавиши ESC
            this.KeyDown += Form_KeyDown;
        }

        // Обработчик нажатия клавиш
        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedItems(sender as ListBox);
                e.Handled = true;
            }
        }

        // Обработчик клика мыши
        private void ListBox_MouseDown(object sender, MouseEventArgs e)
        {
            var listBox = sender as ListBox;

            if (e.Button == MouseButtons.Right)
            {
                // Проверяем, кликнули ли на элемент
                int index = listBox.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    // Если клик был на элементе, который не выделен - выделяем его
                    if (!listBox.SelectedIndices.Contains(index))
                    {
                        listBox.SelectedIndex = index;
                    }

                    // Показываем контекстное меню
                    _contextMenu.Show(listBox, e.Location);
                }
            }
        }

        // Обработчик клика по пункту меню "Удалить"
        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            // Определяем, из какого ListBox было вызвано меню
            var owner = _contextMenu.SourceControl as ListBox;
            if (owner != null)
            {
                DeleteSelectedItems(owner);
            }
        }

        // Метод для удаления выбранных элементов
        private void DeleteSelectedItems(ListBox listBox)
        {
            if (listBox.SelectedItems.Count == 0)
                return;

            // Удаляем в обратном порядке, чтобы индексы не сбивались
            for (int i = listBox.SelectedIndices.Count - 1; i >= 0; i--)
            {
                int index = listBox.SelectedIndices[i];
                string itemToRemove = listBox.Items[index].ToString();
                listBox.Items.RemoveAt(index);

                // Удаляем также из основного списка
                if (itemToRemove != null)
                {
                    _allCharacters.Remove(itemToRemove);
                }
            }
        }
        private void CharacterNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Предотвращаем звуковой сигнал
                _loadButton.PerformClick(); // Нажимаем кнопку "Загрузить"
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    _allCharacters = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    UpdateListBoxes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки конфига: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveConfig()
        {
            try
            {
                // Получаем директорию из пути к файлу
                var directory = Path.GetDirectoryName(_configFilePath);

                // Создаем директорию, если она не существует
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_allCharacters, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения конфига: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateListBoxes()
        {
            _leftListBox.Items.Clear();
            _rightListBox.Items.Clear();

            int halfCount = (int)Math.Ceiling(_allCharacters.Count / 2.0);

            for (int i = 0; i < _allCharacters.Count; i++)
            {
                if (i < halfCount)
                {
                    _leftListBox.Items.Add(_allCharacters[i]);
                }
                else
                {
                    _rightListBox.Items.Add(_allCharacters[i]);
                }
            }
        }

        private void AddCharacters(List<string> newCharacters)
        {
            var uniqueNewCharacters = newCharacters.Except(_allCharacters).ToList();

            if (uniqueNewCharacters.Count > 0)
            {
                _allCharacters.AddRange(uniqueNewCharacters);
                _allCharacters = _allCharacters.Distinct().ToList();
                UpdateListBoxes();

                _statusLabel.Text = $"Добавлено {uniqueNewCharacters.Count} новых персонажей";
                _statusLabel.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                _statusLabel.Text = "Новых персонажей не найдено";
                _statusLabel.ForeColor = System.Drawing.Color.Blue;
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                this.Hide();
            }
        }

        // Добавляем обработчик события показа формы
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                // При показе формы восстанавливаем сохранённые значения
                _allCharacters = new List<string>(_savedCharacters);
                UpdateListBoxes();
                _statusLabel.Text = "Готов к загрузке";
                _statusLabel.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Сохраняем текущие значения как новые сохранённые
            _savedCharacters = new List<string>(_allCharacters);
            SaveConfig();
            this.Hide();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            _allCharacters = new List<string>(_savedCharacters);
            UpdateListBoxes();
            this.Hide();
        }

        private async void LoadButton_Click(object sender, EventArgs e)
        {
            string characterName = _characterNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(characterName))
            {
                MessageBox.Show("Введите имя персонажа", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadCharacterList(characterName);
        }

        private async Task LoadCharacterList(string characterName)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                try
                {
                    _loadButton.Enabled = false;
                    _statusLabel.Text = "Загрузка...";
                    _statusLabel.ForeColor = System.Drawing.Color.Blue;

                    string profileLink = $"https://xn--80aubmleh.xn--p1ai/%D0%9E%D1%80%D1%83%D0%B6%D0%B5%D0%B9%D0%BD%D0%B0%D1%8F/{Uri.EscapeDataString(characterName)}";

                    var response = await httpClient.GetAsync(profileLink);
                    response.EnsureSuccessStatusCode();

                    string htmlContent = await response.Content.ReadAsStringAsync();

                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    List<string> characterNames = ExtractCharacterNames(htmlDoc);
                    AddCharacters(characterNames);

                }
                catch (TaskCanceledException)
                {
                    _statusLabel.Text = "Таймаут загрузки";
                    _statusLabel.ForeColor = System.Drawing.Color.Red;
                }
                catch (Exception ex)
                {
                    _statusLabel.Text = "Ошибка загрузки";
                    _statusLabel.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _loadButton.Enabled = true;
                }
            }
        }

        private List<string> ExtractCharacterNames(HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            var characterNames = new List<string>();

            try
            {
                var serverBlocks = htmlDoc.DocumentNode.SelectNodes("//strong[contains(@class, 'profile-character-list__server')]");

                if (serverBlocks != null)
                {
                    foreach (var serverBlock in serverBlocks)
                    {
                        var charList = serverBlock.SelectSingleNode("./following-sibling::ul[contains(@class, 'profile-character-list__char')]");

                        if (charList != null)
                        {
                            var characterButtons = charList.SelectNodes(".//button[contains(@onclick, '/Profile/Character/')]");

                            if (characterButtons != null)
                            {
                                foreach (var button in characterButtons)
                                {
                                    var onclickAttribute = button.GetAttributeValue("onclick", "");
                                    var match = System.Text.RegularExpressions.Regex.Match(onclickAttribute, @"/Profile/Character/([^']+)");

                                    if (match.Success)
                                    {
                                        characterNames.Add(match.Groups[1].Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (characterNames.Count == 0)
                {
                    var characterButtons = htmlDoc.DocumentNode.SelectNodes("//button[contains(@onclick, '/Profile/Character/')]");
                    if (characterButtons != null)
                    {
                        foreach (var button in characterButtons)
                        {
                            var onclickAttribute = button.GetAttributeValue("onclick", "");
                            var match = System.Text.RegularExpressions.Regex.Match(onclickAttribute, @"/Profile/Character/([^']+)");
                            if (match.Success)
                            {
                                characterNames.Add(match.Groups[1].Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при парсинге HTML: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return characterNames;
        }

        private void CharacterLoaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _httpClient?.Dispose();
        }
    }
}
