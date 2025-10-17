using OpenCvSharp.Features2D;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace OCR_DPS_Monitor
{
    public partial class MainWindow : Form
    {
        private OcrConfig config;

        private LobbyManager _lobbyManager;
        private PartyManager _partyManager;
        private OverlayVisibilityManager _visibilityManager;

        private readonly TextRecognizer _recognizer;
        private readonly System.Timers.Timer _timer;
        private const double timerTick = 1500;
        private readonly object _recognitionLock = new object();
        private bool _isProcessing = false;

        private bool isRunning = false;
        public bool IsOverlayRunning() => isRunning;

        private bool isWindowVisible = false;
        private double currentValue = 0;

        private ManualCharacterLoaderForm _manualCharacterLoaderForm;
        private OverlayBlocksForm _overlayForm;
        

        private ToolStripMenuItem openCloseMenuItem;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private KeyboardHook keyboardHook;
        private const string serverUrl = "https://dps-monitor-lobby-server-production.up.railway.app";
        private const string HotkeyText = " (Ctrl+Shift+➕)";
        private const string HotkeyTextRaid = " (Ctrl + Shift +➖)";
        private Size _realSize = new Size(337, 337); // Ваши реальные размеры по умолчанию

        private IntPtr targetWindowHandle;
        private string executableName = "LOSTARK";

        private List<string> recognizedNicknames = new List<string>();

        const int MDT_EFFECTIVE_DPI = 0;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        
        // Структура RECT
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            // Свойства для удобства
            public int Width => Right - Left;
            public int Height => Bottom - Top;

            // Неявное преобразование в Rectangle
            public static implicit operator Rectangle(RECT rect)
            {
                return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
            }
        }

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        // Функции для получения DPI через GetDeviceCaps
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        // Константы для GetDeviceCaps
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("shcore.dll")]
        static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr hwnd);
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Добавляем стиль инструментального окна
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        public MainWindow()
        {
            //ConsoleHelper.ShowConsole();
            InitializeComponent();
            _realSize = this.Size;
            InitializeTrayIcon();
            InitializeKeyboardHook();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;

            config = OcrConfig.Load();

            // Создаем распознаватель один раз
            _recognizer = new TextRecognizer();


            // Настраиваем таймер (лучше использовать System.Timers.Timer для async)
            _timer = new System.Timers.Timer(timerTick);
            _timer.Elapsed += async (s, e) => await ProcessRecognitionAsync();
            _timer.AutoReset = true;

            ConfigureTransparentForm();

            _manualCharacterLoaderForm = new ManualCharacterLoaderForm();

            InitializeLobbyManager();

            _overlayForm = new OverlayBlocksForm();

            _partyManager = new PartyManager(_overlayForm);

            //_recognizer.TestScenario();
        }

        private void InitializeTrayIcon()
        {
            // Создаем меню
            trayMenu = new ContextMenuStrip();

            // Добавляем пункты меню
            openCloseMenuItem = new ToolStripMenuItem("Открыть" + HotkeyText, null, OnOpenClose);
            trayMenu.Items.Add(openCloseMenuItem);

            trayMenu.Items.Add("О программе", null, OnSettings);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Выход", null, OnExit);

            // Создаем иконку в трее
            trayIcon = new NotifyIcon();
            trayIcon.Text = "OCR ДПС Монитор";
            trayIcon.Icon = new Icon(GetType(), "DPS_Icon.ico");
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            // Обработчики событий
            trayIcon.DoubleClick += (s, e) => ToggleMainWindow();
            trayIcon.MouseUp += TrayIcon_MouseUp;
        }

        private bool IsRunningAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        private void InitializeKeyboardHook()
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Запустите приложение от имени администратора для работы глобальных горячих клавиш");
                return;
            }

            keyboardHook = new KeyboardHook();
            keyboardHook.KeyPressed += (s, e) => ToggleMainWindow();
        }

        private void InitializeLobbyManager()
        {
            _lobbyManager = new LobbyManager();

            // Подписываемся на события лобби
            _lobbyManager.OnLobbyCreated += (lobbyCode) =>
            {
                this.Invoke(new Action(() =>
                {
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Лобби создано\n" + $"Код лобби: {lobbyCode} \nскопирован в буфер обмена");
                    Clipboard.SetText(lobbyCode);
                    ShowLobbyNumber(lobbyCode);
                }));
            };

            _lobbyManager.OnPlayerJoined += (player) =>
            {
                this.Invoke(new Action(() =>
                {
                    // Добавляем игрока в UI
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] {player.name} подключается к лобби");

                    var playerData = new PlayerData
                    {
                        Id = player.id,
                        Name = player.name,
                        PartyNumber = player.partyNumber,
                        ClassType = player.classType,
                        Value = 0,
                        IsConnected = true
                    };

                    _partyManager.AddPlayer(playerData);
                }));
            };

            _lobbyManager.OnPlayerLeft += (playerId, playerName) =>
            {
                this.Invoke(new Action(() =>
                {
                    // Удаляем игрока из UI
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] {playerName} покидает лобби");
                    _partyManager.RemovePlayer(playerId);
                }));
            };

            _lobbyManager.OnAllPlayersData += (players) =>
            {
                this.Invoke(new Action(() =>
                {
                    foreach (var player in players)
                    {
                        var playerData = new PlayerData
                        {
                            Id = player.id,
                            Name = player.name,
                            PartyNumber = player.partyNumber,
                            ClassType = player.classType,
                            Value = 0,
                            IsConnected = true
                        };
                        _partyManager.AddPlayer(playerData);
                        AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] {player.name} подключается к лобби");
                    }
                }));
            };

            _lobbyManager.OnPlayerDisconnected += (value) =>
            {
                this.Invoke(new Action(() =>
                {
                    StopRecognition();
                    _visibilityManager?.Stop();
                    _overlayForm.Hide();
                    SetButtonsEnabled(true);

                    if (btnToggle.Text == "Отключиться")
                    {
                        btnToggle.Text = "Подключиться";
                        btnCreateLobby.Visible = true;
                        //btnToggle_Click(this, EventArgs.Empty);
                    }
                    else if (btnCreateLobby.Text == "Отключиться")
                    {
                        btnCreateLobby.Text = "Создать лобби";
                        btnToggle.Visible = true;
                        //btnCreateLobby_Click(this, EventArgs.Empty);
                    }
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Вы покинули сервер");
                    _partyManager.Disconnect();
                }));
            };

            _lobbyManager.OnJoinError += (error) =>
            {
                this.Invoke(new Action(() =>
                {
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Сервер вернул ошибку: {error}");
                    //if (btnToggle.Text == "Отключиться")
                    //{
                    //    btnToggle_Click(this, EventArgs.Empty);
                    //}
                    //else if (btnCreateLobby.Text == "Отключиться")
                    //{
                    //    btnCreateLobby_Click(this, EventArgs.Empty);
                    //}
                    //_partyManager.Disconnect();
                }));
            };

            // Обработка входящих данных
            _lobbyManager.OnPlayerDataReceived += (data) =>
            {
                this.Invoke(new Action(() =>
                {
                    Debug.WriteLine(data.value);
                    //AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Получено {data.value} от {data.playerId}");
                    // Обрабатываем полученные данные
                    _partyManager.UpdatePlayerData(data.playerId, data.value);
                }));
            };
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                // Показываем меню при любом клике
                var method = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(trayIcon, null);
            }
        }

        private void ShowLobbyNumber(string number)
        {
            var _displayLobbyNumberForm = new DisplayLobbyNumber(number);
            _displayLobbyNumberForm.Show();
        }

        private void ToggleMainWindow()
        {
            if (isWindowVisible)
            {
                HideMainWindow();
            }
            else
            {
                ShowMainWindow();
            }
        }

        private void OnOpenClose(object sender, EventArgs e)
        {
            ToggleMainWindow();
        }

        private void OnSettings(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this); // Показываем как модальное окно
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon?.Dispose();
            keyboardHook?.Dispose();
            _timer?.Stop();
            _timer?.Dispose();
            _recognizer?.Dispose();
            _visibilityManager?.Dispose();

            Application.Exit();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated && value)
            {
                CreateHandle();
                // Сразу скрываем из таскбара при первом показе
                if (this.WindowState == FormWindowState.Minimized)
                {
                    base.SetVisibleCore(false);
                    return;
                }
            }
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.WindowState = FormWindowState.Minimized;
            }
            base.OnFormClosing(e);
        }

        private void UpdateTrayMenuText()
        {
            if (openCloseMenuItem != null)
            {
                openCloseMenuItem.Text = isWindowVisible ? "Скрыть" + HotkeyText : "Открыть" + HotkeyText;
            }
        }
        private void ShowMainWindow()
        {
            this.Show();
            this.ShowInTaskbar = false;

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }

            // Обновляем позиционирование после показа
            PositionFormBottomRightWithCheck();

            isWindowVisible = true;
            UpdateTrayMenuText();
        }

        private void HideMainWindow()
        {
            this.Hide();
            isWindowVisible = false;
            UpdateTrayMenuText();
        }

        private void PositionFormBottomRightWithCheck()
        {
            // Проверяем, что координаты валидные
            if (this.Location.X < -30000 || this.Location.Y < -30000)
            {
                // Если координаты все еще невалидные, ждем еще
                this.BeginInvoke(new Action(PositionFormBottomRightWithCheck));
                return;
            }

            PositionFormBottomRight();
        }
        private void ConfigureButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.White;
            button.ForeColor = Color.White;
            button.BackColor = Color.Transparent;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
        }

        private void ConfigureTransparentForm()
        {
            // Делаем форму прозрачной
            this.BackColor = Color.LimeGreen; // Цвет, который будет прозрачным
            this.TransparencyKey = Color.LimeGreen;
            this.FormBorderStyle = FormBorderStyle.None;

            ConfigureButton(btnToggle);
            ConfigureButton(btnSetDps);
            ConfigureButton(btnSetRegion);
            ConfigureButton(btnSetLegacy);
            ConfigureButton(btnRaidParty);
            ConfigureButton(btnCreateLobby);

            // Настраиваем цвета для разных состояний (делаем их прозрачными)
            //btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
            //btnSetRegion.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
            //btnSetDps.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
            //btnSetLegacy.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
            //btnCreateLobby.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);
            //btnRaidParty.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, Color.Transparent);

            //// Делаем кнопки прозрачными с видимым текстом и рамкой
            //btnToggle.FlatAppearance.BorderSize = 1;
            //btnToggle.FlatAppearance.BorderColor = Color.White;
            //btnToggle.ForeColor = Color.White;
            //btnToggle.BackColor = Color.Transparent;

            //btnSetRegion.FlatAppearance.BorderSize = 1;
            //btnSetRegion.FlatAppearance.BorderColor = Color.White;
            //btnSetRegion.ForeColor = Color.White;
            //btnSetRegion.BackColor = Color.Transparent;

            //btnSetDps.FlatAppearance.BorderSize = 1;
            //btnSetDps.FlatAppearance.BorderColor = Color.White;
            //btnSetDps.ForeColor = Color.White;
            //btnSetDps.BackColor = Color.Transparent;

            //btnSetLegacy.FlatAppearance.BorderSize = 1;
            //btnSetLegacy.FlatAppearance.BorderColor = Color.White;
            //btnSetLegacy.ForeColor = Color.White;
            //btnSetLegacy.BackColor = Color.Transparent;

            //btnCreateLobby.FlatAppearance.BorderSize = 1;
            //btnCreateLobby.FlatAppearance.BorderColor = Color.White;
            //btnCreateLobby.ForeColor = Color.White;
            //btnCreateLobby.BackColor = Color.Transparent;

            //btnRaidParty.FlatAppearance.BorderSize = 1;
            //btnRaidParty.FlatAppearance.BorderColor = Color.White;
            //btnRaidParty.ForeColor = Color.White;
            //btnRaidParty.BackColor = Color.Transparent;

            // Настраиваем textBoxDebug
            richTextBoxDebug.BackColor = Color.LimeGreen; // Тот же цвет, что и TransparencyKey
            richTextBoxDebug.ForeColor = Color.White;
            richTextBoxDebug.BorderStyle = BorderStyle.None;
            richTextBoxDebug.ScrollBars = RichTextBoxScrollBars.None;
            richTextBoxDebug.Font = new Font("Consolas", 10);

            // Делаем форму поверх всех окон
            this.TopMost = true;
        }

        private void AppendTextWithAutoScroll(string text)
        {
            richTextBoxDebug.AppendText(text + Environment.NewLine);

            // Автоматическая прокрутка к концу
            richTextBoxDebug.SelectionStart = richTextBoxDebug.Text.Length;
            richTextBoxDebug.ScrollToCaret();
        }


        private void PositionFormBottomRight()
        {

            // Ждем, пока форма полностью инициализируется
            if (this.IsHandleCreated && this.Visible)
            {
                int targetWidth = _realSize.Width;
                int targetHeight = _realSize.Height;

                // Получаем рабочий area текущего экрана (учитывает панель задач)
                Rectangle workingArea = Screen.GetWorkingArea(this);

                // Устанавливаем позицию с учетом реальных размеров формы
                this.Location = new Point(
                    workingArea.Right - targetWidth - 10,
                    workingArea.Bottom - targetHeight - 10
                );
            }
            else
            {
                // Если форма еще не готова, отложим позиционирование
                this.Shown += (s, args) => PositionFormBottomRight();
            }
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {

        }

        private bool CanStartMonitoring()
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            bool canStart = true;
            string errorMessage = "";

            // Проверка региона окна рейда
            //if (config.Region.IsEmpty || config.Region.Width <= 0 || config.Region.Height <= 0)
            //{
            //    errorMessage += $"[{timestamp}] Область расположения Окна Рейда не задана\n";
            //    canStart = false;
            //}

            // Проверка региона ДПС метра
            if (config.DpsRegion.IsEmpty || config.DpsRegion.Width <= 0 || config.DpsRegion.Height <= 0)
            {
                errorMessage += $"[{timestamp}] Область расположения ДПС Метра не задана\n";
                canStart = false;
            }

            // Проверка списка персонажей
            var characters = _manualCharacterLoaderForm?.GetAllCharacters();
            if (characters == null || characters.Count == 0)
            {
                errorMessage += $"[{timestamp}] Список персонажей наследия пуст\n";
                canStart = false;
            }

            // Выводим сообщения об ошибках, если есть
            if (!canStart && !string.IsNullOrEmpty(errorMessage))
            {
                AppendTextWithAutoScroll(errorMessage);
            }

            return canStart;
        }


        private IntPtr FindProcessWindowByExecutable(string executableName)
        {
            try
            {
                // Ищем все процессы, имя которых содержит указанное имя
                var processes = Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains(executableName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var process in processes)
                {
                    // Проверяем, что у процесса есть главное окно и оно видимо
                    if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
                    {
                        return process.MainWindowHandle;
                    }
                }

                // Альтернативный метод: перебираем все окна
                return FindWindowByProcessName(executableName);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr FindWindowByProcessName(string processName)
        {
            IntPtr foundWindow = IntPtr.Zero;

            // Перебираем все окна и ищем те, которые принадлежат процессу с нужным именем
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                GetWindowThreadProcessId(hWnd, out uint processId);
                try
                {
                    using (var process = Process.GetProcessById((int)processId))
                    {
                        if (process.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase))
                        {
                            foundWindow = hWnd;
                            return false; // Прерываем перебор
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки
                }
                return true;
            }, IntPtr.Zero);

            return foundWindow;
        }

        private bool IsLostArkProcessRunning()
        {
            targetWindowHandle = FindProcessWindowByExecutable(executableName);
            return targetWindowHandle != IntPtr.Zero;
        }

        private void LostArkIsNotRunning()
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string errorMessage = $"[{timestamp}] Процесс LOSTARK.exe не запущен";
            AppendTextWithAutoScroll(errorMessage);
        }


        //Распознавание лобби рейда
        //private bool AnalyzeLobbyAndDisplayResults()
        //{
        //    Rectangle rect = config.Region;
        //    var processor = new ImageProcessor();
        //    var recognizer = new TextRecognizer();
        //    Color targetColor = Color.FromArgb(0xD1, 0x97, 0xF5);

        //    using (Bitmap screenCapture = CaptureScreenRegion(rect))
        //    {
        //        // Обрабатываем изображение
        //        using (Bitmap processedImage = processor.RemoveAreasByColor(screenCapture, targetColor, 15))
        //        {
        //            //OpenBitmap(processedImage);
        //            var nicknames = recognizer.RecognizeNicknames(processedImage);

        //            recognizedNicknames.Clear();

        //            string timestamp = DateTime.Now.ToString("HH:mm:ss");
        //            StringBuilder resultBuilder = new StringBuilder();

        //            if (nicknames == null || nicknames.Count == 0)
        //            {
        //                resultBuilder.AppendLine($"[{timestamp}] Лобби рейда не найдено");
        //                AppendTextWithAutoScroll(resultBuilder.ToString());
        //                return false;
        //            }

        //            if (nicknames.Count == 4 || nicknames.Count == 8)
        //            {
        //                recognizedNicknames.AddRange(nicknames);
        //                resultBuilder.AppendLine($"[{timestamp}] Распознано {nicknames.Count} участников рейда");

        //                // Вывод никнеймов
        //                if (nicknames.Count == 4)
        //                {
        //                    // По одному никнейму в строке
        //                    foreach (var nickname in nicknames)
        //                    {
        //                        resultBuilder.AppendLine(nickname);
        //                    }
        //                }
        //                else // 8 никнеймов
        //                {
        //                    int halfCount = nicknames.Count / 2;
        //                    int maxLength = nicknames.Take(halfCount).Max(nick => nick.Length);
        //                    int columnWidth = maxLength + 1; // Отступ между колонками

        //                    for (int i = 0; i < halfCount; i++)
        //                    {
        //                        string firstNick = nicknames[i];
        //                        string secondNick = nicknames[i + halfCount];

        //                        string line = $"{firstNick.PadRight(columnWidth)}{secondNick}";
        //                        resultBuilder.AppendLine(line);
        //                    }

        //                }

        //                AppendTextWithAutoScroll(resultBuilder.ToString());
        //                return true;
        //            }
        //            else
        //            {
        //                // Объединяем сообщения об ошибке в один вызов
        //                resultBuilder.AppendLine($"[{timestamp}] Ошибка распознавания участников рейда");
        //                resultBuilder.AppendLine($"[{timestamp}] Распознано {nicknames.Count} участников");

        //                // Вывод всех никнеймов по 2 в строке
        //                for (int i = 0; i < nicknames.Count; i += 2)
        //                {
        //                    if (i + 1 < nicknames.Count)
        //                    {
        //                        string line = $"{nicknames[i].PadRight(20)} {nicknames[i + 1]}";
        //                        resultBuilder.AppendLine(line);
        //                    }
        //                    else
        //                    {
        //                        resultBuilder.AppendLine(nicknames[i]);
        //                    }
        //                }

        //                AppendTextWithAutoScroll(resultBuilder.ToString());
        //                return false;
        //            }
        //        }
        //    }
        //}

        private async Task ProcessRecognitionAsync()
        {
            // Защита от параллельного выполнения
            if (_isProcessing) return;

            lock (_recognitionLock)
            {
                if (_isProcessing) return;
                _isProcessing = true;
            }

            try
            {
                await Task.Run(() =>
                {
                    if (targetWindowHandle == IntPtr.Zero ||
                        !IsWindowVisible(targetWindowHandle) ||
                        IsIconic(targetWindowHandle))
                    {
                        return; // Просто выходим без исключений
                    }

                    Rectangle rect = config.DpsRegion;
                    using (Bitmap bmp = CaptureScreenRegion(rect))
                    {
                        double? result = _recognizer.RecognizeNumber(bmp);

                        // Обновляем UI через Invoke
                        this.Invoke(new Action(() =>
                        {
                            //AppendTextWithAutoScroll($"Распознано: {result.ToString()}");
                            if (result.HasValue)
                            {
                                //AppendTextWithAutoScroll($"Распознано: {result.ToString()}");
                                if (result.Value != 0 && result.Value != currentValue)
                                {
                                    _partyManager.UpdatePlayerData(_lobbyManager.CurrentPlayerId, result.Value);
                                    _ = Task.Run(async () => await _lobbyManager.SendPlayerData(result.Value));
                                    currentValue = result.Value;
                                }
                            }
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в таймере: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }


        // Запуск/остановка распознавания
        public void StartRecognition() => _timer.Start();
        public void StopRecognition() => _timer.Stop();

        private void SetButtonsEnabled(bool enabled)
        {
            btnToggle.Enabled = enabled;
            btnSetDps.Enabled = enabled;
            btnSetLegacy.Enabled = enabled;
            btnCreateLobby.Enabled = enabled;
            btnRaidParty.Enabled = enabled;
        }


        private async Task<bool> PreLobbyChecks()
        {
            if (!CanStartMonitoring())
            {
                return false;
            }

            if (!IsLostArkProcessRunning())
            {
                LostArkIsNotRunning();
                return false;
            }

            return true;
        }

        private async Task ConnectToServer()
        {
            AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Подключаемся к серверу...");
            await _lobbyManager.ConnectToServer(serverUrl);
            AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Соединение с сервером установлено");
        }

        private async Task CreateLobbyOperation()
        {
            using (var form = new CreateLobbyForm(_manualCharacterLoaderForm.GetAllCharacters(), LobbyFormMode.Create))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Создаём лобби...");
                    await _lobbyManager.CreateLobby(
                        form.Result.CharacterName,
                        form.Result.PartyNumber,
                        form.Result.ClassType);
                    // Лобби создано успешно
                }
                else
                {
                    throw new OperationCanceledException("User cancel");
                }
            }
        }
        private async Task JoinLobbyOperation()
        {
            var form = new CreateLobbyForm(_manualCharacterLoaderForm.GetAllCharacters(), LobbyFormMode.Join);
            if (form.ShowDialog() == DialogResult.OK)
            {
                AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Подключаемся к лобби...");
                await _lobbyManager.JoinLobby(
                    form.Result.LobbyCode,
                    form.Result.CharacterName,
                    form.Result.PartyNumber,
                    form.Result.ClassType);
                // Подключение успешно
                
            }
            else
            {
                throw new OperationCanceledException("User cancel");
            }
        }
        private void InitializeVisibilityManager()
        {
            if (_visibilityManager == null)
            {
                _visibilityManager = new OverlayVisibilityManager(this, _overlayForm);
            }

            _visibilityManager.Initialize(targetWindowHandle);

            //_visibilityManager = new OverlayVisibilityManager(targetWindowHandle, this);
        }

        private async Task<bool> HandleLobbyOperation(LobbyFormMode operationType)
        {
            if (!await PreLobbyChecks()) return false;

            try
            {
                await ConnectToServer();

                if (operationType == LobbyFormMode.Create)
                {
                    await CreateLobbyOperation();
                    btnCreateLobby.Text = "Отключиться";
                    btnToggle.Visible = false;
                }
                else
                {
                    await JoinLobbyOperation();
                    btnToggle.Text = "Отключиться";
                    btnCreateLobby.Visible = false;
                }

                //SetButtonsEnabled(true);

                // Общая логика после успешной операции
                StartRecognition();
                _overlayForm.Show();
                HideMainWindow();
                InitializeVisibilityManager();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                {
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Ошибка: {ex.Message}");
                }
                else
                {
                    AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Соединение завершено");
                }
                await _lobbyManager.Disconnect();

                return false;
            }

            return true;
        }



        private async void btnToggle_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            isRunning = !isRunning;
            if (isRunning)
            {
                if (!await HandleLobbyOperation(LobbyFormMode.Join))
                {
                    isRunning = false;
                    SetButtonsEnabled(true);
                    return;
                }
                SetButtonsEnabled(true);
            }
            else
            {
                await _lobbyManager.Disconnect();
            }
        }

        // Получаем DPI масштаб для окна
        //private static float GetWindowDpiScale(IntPtr hwnd)
        //{
        //    try
        //    {
        //        var dpi = GetDpiForWindow(hwnd);
        //        return dpi / 96.0f;
        //    }
        //    catch
        //    {
        //        // Fallback: получаем DPI для основного монитора
        //        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
        //        {
        //            return g.DpiX / 96.0f;
        //        }
        //    }
        //}

        static float GetWindowDpiScale(IntPtr hwnd)
        {
            float scale = 1.0f;

            try
            {
                // Пробуем современный API
                IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    uint dpiX, dpiY;
                    int result = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);

                    if (result == 0 && dpiX > 0) // SUCCEEDED и валидный DPI
                    {
                        scale = dpiX / 96.0f;
                        Console.WriteLine($"DPI from monitor: {dpiX}, scale: {scale}");
                        return scale;
                    }
                }
            }
            catch (DllNotFoundException)
            {
                Console.WriteLine("Shcore.dll not found (older Windows)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDpiForMonitor failed: {ex.Message}");
            }

            // Fallback 1: Через Graphics
            try
            {
                using (Graphics g = Graphics.FromHwnd(hwnd))
                {
                    scale = g.DpiX / 96.0f;
                    Console.WriteLine($"DPI from Graphics: {g.DpiX}, scale: {scale}");

                    if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale))
                    {
                        scale = 1.0f;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Graphics.DpiX failed: {ex.Message}");
            }

            // Fallback 2: Через системные настройки
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                int dpi = GetDeviceCaps(hdc, 88); // LOGPIXELSX
                ReleaseDC(IntPtr.Zero, hdc);

                if (dpi > 0)
                {
                    scale = dpi / 96.0f;
                    Console.WriteLine($"DPI from system: {dpi}, scale: {scale}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System DPI failed: {ex.Message}");
            }

            Console.WriteLine($"Final scale: {scale}");
            return scale;
        }

        //static Bitmap CaptureWindowRegion(IntPtr windowHandle, Rectangle originalRect)
        //{
        //    // Получаем DPI масштаб для окна
        //    float dpiScale = GetWindowDpiScale(windowHandle);

        //    // Масштабируем координаты и размеры
        //    Rectangle rect = new Rectangle(
        //        (int)(originalRect.X * dpiScale),
        //        (int)(originalRect.Y * dpiScale),
        //        (int)(originalRect.Width * dpiScale),
        //        (int)(originalRect.Height * dpiScale)
        //    );

        //    IntPtr windowDC = GetWindowDC(windowHandle);
        //    IntPtr memDC = CreateCompatibleDC(windowDC);
        //    IntPtr hBitmap = CreateCompatibleBitmap(windowDC, rect.Width, rect.Height);
        //    IntPtr oldObj = SelectObject(memDC, hBitmap);

        //    // Копируем из окна, а не из всего экрана
        //    bool printSuccess = PrintWindow(windowHandle, memDC, 0);
        //    if (printSuccess)
        //    {
        //        // PrintWindow сам заполнил memDC - используем его результат
        //    }
        //    else
        //    {
        //        BitBlt(memDC, 0, 0, rect.Width, rect.Height, windowDC, originalRect.X, originalRect.Y, CopyPixelOperation.SourceCopy);
        //    }

        //    Bitmap bmp = Image.FromHbitmap(hBitmap);

        //    SelectObject(memDC, oldObj);
        //    DeleteObject(hBitmap);
        //    DeleteDC(memDC);
        //    ReleaseDC(windowHandle, windowDC);

        //    return bmp;
        //}


        //static Bitmap CaptureWindowRegion(IntPtr windowHandle, Rectangle originalRect)
        //{
        //    // Получаем размеры всего окна
        //    RECT windowRect;
        //    GetClientRect(windowHandle, out windowRect);

        //    Console.WriteLine($"Window size: {windowRect.Width}x{windowRect.Height}");
        //    Console.WriteLine($"Requested region: {originalRect}");

        //    float dpiScale = GetWindowDpiScale(windowHandle);
        //    Rectangle targetRect = new Rectangle(
        //        originalRect.X,
        //        originalRect.Y,
        //        (int)(originalRect.Width * dpiScale),
        //        (int)(originalRect.Height * dpiScale)
        //    );

        //    Console.WriteLine($"Target region (scaled): {targetRect}");

        //    // Захватываем ВСЁ окно через PrintWindow
        //    IntPtr windowDC = GetWindowDC(windowHandle);
        //    if (windowDC == IntPtr.Zero)
        //    {
        //        Console.WriteLine("GetWindowDC failed!");
        //        return null;
        //    }

        //    IntPtr memDC = CreateCompatibleDC(windowDC);
        //    IntPtr hBitmap = CreateCompatibleBitmap(windowDC, windowRect.Right, windowRect.Bottom);
        //    IntPtr oldObj = SelectObject(memDC, hBitmap);

        //    // Очищаем bitmap (на случай если PrintWindow не сработает)
        //    using (Graphics g = Graphics.FromHdc(memDC))
        //    {
        //        g.Clear(Color.Black);
        //    }

        //    bool printSuccess = PrintWindow(windowHandle, memDC, 0);
        //    Console.WriteLine($"PrintWindow result: {printSuccess}");

        //    Bitmap fullWindowBmp = Image.FromHbitmap(hBitmap);
        //    Console.WriteLine($"Full window captured: {fullWindowBmp.Width}x{fullWindowBmp.Height}");

        //    // Проверяем, что целевая область внутри изображения
        //    if (targetRect.X >= fullWindowBmp.Width || targetRect.Y >= fullWindowBmp.Height)
        //    {
        //        Console.WriteLine("Target region outside window bounds!");
        //        fullWindowBmp.Dispose();
        //        SelectObject(memDC, oldObj);
        //        DeleteObject(hBitmap);
        //        DeleteDC(memDC);
        //        ReleaseDC(windowHandle, windowDC);
        //        return null;
        //    }

        //    // Корректируем размер если нужно
        //    if (targetRect.X + targetRect.Width > fullWindowBmp.Width)
        //        targetRect.Width = fullWindowBmp.Width - targetRect.X;
        //    if (targetRect.Y + targetRect.Height > fullWindowBmp.Height)
        //        targetRect.Height = fullWindowBmp.Height - targetRect.Y;

        //    Console.WriteLine($"Final crop region: {targetRect}");

        //    // Обрезаем нужную область
        //    Bitmap croppedBmp = new Bitmap(targetRect.Width, targetRect.Height);
        //    using (Graphics g = Graphics.FromImage(croppedBmp))
        //    {
        //        g.DrawImage(fullWindowBmp,
        //            new Rectangle(0, 0, targetRect.Width, targetRect.Height),
        //            targetRect,
        //            GraphicsUnit.Pixel);
        //    }

        //    fullWindowBmp.Dispose();

        //    SelectObject(memDC, oldObj);
        //    DeleteObject(hBitmap);
        //    DeleteDC(memDC);
        //    ReleaseDC(windowHandle, windowDC);

        //    Console.WriteLine($"Final image: {croppedBmp.Width}x{croppedBmp.Height}");
        //    return croppedBmp;
        //}


        static Bitmap CaptureScreenRegion(Rectangle rect)
        {
            IntPtr desktopWnd = GetDesktopWindow();
            IntPtr desktopDC = GetWindowDC(desktopWnd);
            IntPtr memDC = CreateCompatibleDC(desktopDC);
            IntPtr hBitmap = CreateCompatibleBitmap(desktopDC, rect.Width, rect.Height);
            IntPtr oldObj = SelectObject(memDC, hBitmap);

            BitBlt(memDC, 0, 0, rect.Width, rect.Height, desktopDC, rect.X, rect.Y, CopyPixelOperation.SourceCopy);

            Bitmap bmp = Image.FromHbitmap(hBitmap);

            SelectObject(memDC, oldObj);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(desktopWnd, desktopDC);

            return bmp;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

            this.Shown += (s, args) => PositionFormBottomRight();
        }

        private void SetRegion(Func<Rectangle> getRegion, Action<Rectangle> setRegion, string successMessage)
        {
            Rectangle oldRegion = getRegion(); // Сохраняем старые координаты

            this.Hide();
            using (var overlay = new OverlayForm())
            {
                if (overlay.ShowDialog() == DialogResult.OK)
                {
                    Rectangle newRegion = overlay.SelectedRegion;

                    // Проверяем, изменились ли координаты
                    if (newRegion != oldRegion)
                    {
                        setRegion(newRegion);
                        config.Save();

                        AppendRegionCoordinatesDetailed(newRegion, successMessage);
                    }
                    else
                    {
                        AppendTextWithAutoScroll($"[{DateTime.Now:HH:mm:ss}] Координаты не изменились");
                    }
                }
            }
            this.Show();
        }

        private void AppendRegionCoordinatesDetailed(Rectangle region, string title)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string coordinates = $"[{timestamp}] {title}: " +
                               $"({region.X},{region.Y},{region.Right},{region.Bottom})";
            AppendTextWithAutoScroll(coordinates);
        }

        private void btnSetRegion_Click(object sender, EventArgs e)
        {
            SetRegion(
                () => config.Region,
                region => config.Region = region,
                "Координаты окна рейда сохранены"
            );
        }

        private void btnSetDps_Click(object sender, EventArgs e)
        {
            SetRegion(
                () => config.DpsRegion,
                region => config.DpsRegion = region,
                "Координаты ДПС метра сохранены"
            );
        }

        private void btnSetLegacy_Click(object sender, EventArgs e)
        {
            _manualCharacterLoaderForm.Show();


            //OnSettings(sender, e);

            //using (Bitmap bmp = CaptureScreenRegion(config.DpsRegion))
            //{
            //    double? result = _recognizer.RecognizeNumber(bmp);
            //}
        }

        private async void btnCreateLobby_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            isRunning = !isRunning;
            if (isRunning)
            {
                if (!await HandleLobbyOperation(LobbyFormMode.Create))
                {
                    isRunning = false;
                    SetButtonsEnabled(true);
                    return;
                }
                SetButtonsEnabled(true);
            }
            else
            {
                await _lobbyManager.Disconnect();
            }

        }

        private void btnRaidParty_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            HideMainWindow();

            _overlayForm.EditCompleted += OverlayForm_EditCompleted;
            _overlayForm.Show();
            _overlayForm.EditMode = !_overlayForm.EditMode;
        }

        private void OverlayForm_EditCompleted(object sender, EventArgs e)
        {
            _overlayForm.EditCompleted -= OverlayForm_EditCompleted;
            _overlayForm.EditMode = !_overlayForm.EditMode;
            if (!isRunning)
            {
                _overlayForm.Hide();
                ShowMainWindow();
            }
            SetButtonsEnabled(true);
        }
    }
}