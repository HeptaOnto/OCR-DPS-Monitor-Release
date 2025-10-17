using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace OCR_DPS_Monitor
{
    public partial class ManualCharacterLoaderForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly string _configFilePath;
        private List<string> _allCharacters = new List<string>();
        private List<string> _savedCharacters = new List<string>(); // Сохранённые значения

        public ManualCharacterLoaderForm()
        {
            InitializeComponent();
            AttachEventHandlers();
            _httpClient = new HttpClient();
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "legacy_data.json");

            LoadConfig();

            _savedCharacters = new List<string>(_allCharacters); // Сохраняем начальные значения
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        private void AttachEventHandlers()
        {
            _characterNameTextBox.KeyDown += CharacterNameTextBox_KeyDown;
            _loadButton.Click += LoadButton_Click;
            _saveButton.Click += SaveButton_Click;
            _cancelButton.Click += CancelButton_Click;
            _leftListBox.KeyDown += ListBox_KeyDown;
            _leftListBox.MouseDown += ListBox_MouseDown;
            _rightListBox.KeyDown += ListBox_KeyDown;
            _rightListBox.MouseDown += ListBox_MouseDown;
            deleteMenuItem.Click += DeleteMenuItem_Click;
            this.KeyDown += Form_KeyDown;
            this.KeyPreview = true;
            this.FormClosing += ManualCharacterLoaderForm_FormClosing;
        }

        public List<string> GetAllCharacters()
        {
            return new List<string>(_allCharacters); // Возвращаем копию
        }

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
        //private void DeleteSelectedItems(ListBox listBox)
        //{            if (listBox.SelectedItems.Count == 0)
        //        return;

        //    // Удаляем в обратном порядке, чтобы индексы не сбивались
        //    for (int i = listBox.SelectedIndices.Count - 1; i >= 0; i--)
        //    {
        //        int index = listBox.SelectedIndices[i];
        //        string itemToRemove = listBox.Items[index].ToString();
        //        listBox.Items.RemoveAt(index);

        //        // Удаляем также из основного списка
        //        if (itemToRemove != null)
        //        {
        //            _allCharacters.Remove(itemToRemove);
        //        }
        //    }
        //}

        private void DeleteSelectedItems(ListBox listBox)
        {
            if (listBox.SelectedItems.Count == 0)
                return;

            // Создаем копию выбранных элементов
            var selectedItems = new List<object>();
            selectedItems.AddRange(listBox.SelectedItems.Cast<object>());

            // Удаляем из основного списка
            foreach (object item in selectedItems)
            {
                string itemString = item.ToString();
                _allCharacters.Remove(itemString);
            }

            // Удаляем из ListBox
            foreach (object item in selectedItems)
            {
                listBox.Items.Remove(item);
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
        private void ManualCharacterLoaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _httpClient?.Dispose();
        }
    }
}
