using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OCR_DPS_Monitor
{
    public partial class OverlayBlocksForm : Form
    {
        private bool _editMode = false;
        public event EventHandler EditCompleted;

        private List<PlayerBlock> _blocks = new List<PlayerBlock>();
        private PlayerBlock _draggedBlock = null;
        private PlayerBlock _contextMenuBlock = null;
        private Point _dragOffset;

        private PartyManager _partyManager;

        private string configPath = Path.Combine(Application.StartupPath, "config", "raidConfig.json");

        // Контекстное меню
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _verticalItem;
        private ToolStripMenuItem _horizontalItem;

        // Для сквозного клика
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Добавляем стиль WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x80; // 0x80 = WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && _editMode)
            {
                // Выход из режима редактирования с сохранением
                SaveConfiguration();
                EditCompleted?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Сохранение конфигурации
        private void SaveConfiguration()
        {
            try
            {
                var config = new List<BlockConfig>();
                foreach (var block in _blocks)
                {
                    config.Add(new BlockConfig
                    {
                        X = block.Position.X,
                        Y = block.Position.Y,
                        Orientation = block.Orientation
                    });
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var directory = Path.GetDirectoryName(configPath);

                // Создаем директорию, если она не существует
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения конфигурации: {ex.Message}");
            }
        }

        // Загрузка конфигурации
        private bool LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath))
                    return false;

                string json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var config = JsonSerializer.Deserialize<List<BlockConfig>>(json, options);

                if (config == null || config.Count == 0)
                    return false;

                // Создаем стандартные блоки
                InitializeBlocks();

                // Применяем сохраненные позиции и ориентацию
                for (int i = 0; i < Math.Min(config.Count, _blocks.Count); i++)
                {
                    var blockConfig = config[i];
                    _blocks[i].Position = new Point(blockConfig.X, blockConfig.Y);
                    _blocks[i].Orientation = blockConfig.Orientation;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
                return false;
            }
        }
    
        public bool EditMode
        {
            get => _editMode;
            set
            {
                if (_editMode != value)
                {
                    _editMode = value;

                    // При выходе из режима редактирования сохраняем конфигурацию
                    if (!_editMode)
                    {
                        SaveConfiguration();
                    }

                    UpdateFormProperties();
                    this.Invalidate();
                }
            }
        }

        public OverlayBlocksForm()
        {
            InitializeForm();
            InitializeContextMenu();

            _partyManager = new PartyManager(this);

            if (!LoadConfiguration())
            {
                InitializeBlocks();
            }

            UpdateFormProperties();
        }

        private void InitializeForm()
        {
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            // Устанавливаем размер формы на весь экран
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
        }

        private void InitializeBlocks()
        {
            // Цвета для блоков
            string[] colors1 = { "#249054", "#6b4920", "#592754", "#283e6d" };
            string[] colors2 = { "#1a804a", "#5a3d1a", "#491d50", "#20325a" };
            string[] colors3 = { "#308a60", "#7a5930", "#693764", "#384e7d" };
            string[] colors4 = { "#1e8a4a", "#61411a", "#51275c", "#243662" };

            // Создаем два блока
            using (var tempGraphics = this.CreateGraphics())
            {
                _blocks.Add(new PlayerBlock(colors1, 0, tempGraphics));
                _blocks.Add(new PlayerBlock(colors2, 1, tempGraphics));
            }

            // Начальное расположение
            ArrangeBlocksVertically();
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            _verticalItem = new ToolStripMenuItem("Вертикальное расположение");
            _verticalItem.Click += (s, e) =>
            {
                if (_contextMenuBlock != null)
                {
                    // Меняем только ориентацию текущего блока, не трогая расположение
                    _contextMenuBlock.Orientation = Orientation.Vertical;
                    this.Invalidate(); // Просто перерисовываем
                }
            };

            _horizontalItem = new ToolStripMenuItem("Горизонтальное расположение");
            _horizontalItem.Click += (s, e) =>
            {
                if (_contextMenuBlock != null)
                {
                    // Меняем только ориентацию текущего блока, не трогая расположение
                    _contextMenuBlock.Orientation = Orientation.Horizontal;
                    this.Invalidate(); // Просто перерисовываем
                }
            };

            _contextMenu.Items.AddRange(new ToolStripItem[] { _verticalItem, _horizontalItem });
        }

        private void UpdateFormProperties()
        {
            if (!_editMode)
            {
                // В обычном режиме - сквозные клики
                int style = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            }
            else
            {
                // В режиме редактирования - нормальные клики
                int style = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Устанавливаем высокое качество рендеринга для лучшего отображения на высоком DPI
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            foreach (var block in _blocks)
            {
                block.Draw(e.Graphics);
            }

            // В режиме редактирования рисуем рамку вокруг блоков
            if (_editMode)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

                // Масштабируем толщину пера и размер inflate
                float penWidth = 2 * e.Graphics.DpiX / 96.0f;
                int inflateSize = (int) (2 * e.Graphics.DpiX / 96.0f);

                using (var pen = new Pen(Color.White, penWidth) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    foreach (var block in _blocks)
                    {
                        var bounds = block.Bounds;
                        bounds.Inflate(inflateSize, inflateSize);
                        e.Graphics.DrawRectangle(pen, bounds);
                    }
                }

                DrawHint(e.Graphics);
            }
        }

        private void DrawHint(Graphics graphics)
        {
            string hintText = "ESC - сохранить и выйти";

            using (var textBrush = new SolidBrush(Color.White))
            using (var outlineBrush = new SolidBrush(Color.Black))
            using (var font = new Font("Segoe UI", 50 * graphics.DpiY / 96.0f, FontStyle.Bold))
            {
                PointF center = new PointF(
                    ClientSize.Width / 2,
                    ClientSize.Height / 2
                );

                SizeF textSize = graphics.MeasureString(hintText, font);
                PointF textLocation = new PointF(
                    center.X - textSize.Width / 2,
                    center.Y - textSize.Height / 2
                );

                // Рисуем тонкую обводку для лучшей читаемости
                //for (int i = -1; i <= 1; i++)
                //{
                //    for (int j = -1; j <= 1; j++)
                //    {
                //        if (i != 0 || j != 0)
                //        {
                //            graphics.DrawString(hintText, font, outlineBrush,
                //                textLocation.X + i, textLocation.Y + j);
                //        }
                //    }
                //}

                // Рисуем основной текст
                graphics.DrawString(hintText, font, textBrush, textLocation);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!_editMode) return;

            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                foreach (var block in _blocks)
                {
                    if (block.Bounds.Contains(e.Location))
                    {
                        _draggedBlock = block;
                        _dragOffset = new Point(e.X - block.Position.X, e.Y - block.Position.Y);
                        break;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_editMode || _draggedBlock == null) return;

            _draggedBlock.Position = new Point(e.X - _dragOffset.X, e.Y - _dragOffset.Y);
            this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _draggedBlock = null;
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            if (!_editMode) return;

            var mousePos = this.PointToClient(Cursor.Position);

            foreach (var block in _blocks)
            {
                if (block.Bounds.Contains(mousePos))
                {
                    // Просто меняем ориентацию, не меняя расположение
                    block.Orientation = block.Orientation == Orientation.Horizontal ?
                        Orientation.Vertical : Orientation.Horizontal;

                    this.Invalidate();
                    break;
                }
            }
        }

        private void ShowContextMenu(Point location)
        {
            _contextMenuBlock = null;

            foreach (var block in _blocks)
            {
                if (block.Bounds.Contains(location))
                {
                    _contextMenuBlock = block;
                    break;
                }
            }

            if (_contextMenuBlock != null)
            {
                // Обновляем чекбоксы в меню
                _verticalItem.Checked = _contextMenuBlock.Orientation == Orientation.Vertical;
                _horizontalItem.Checked = _contextMenuBlock.Orientation == Orientation.Horizontal;

                _contextMenu.Show(this, location);
            }
        }

        public void ArrangeBlocksVertically()
        {
            int spacing = 20;

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (i == 0)
                {
                    // Первый блок оставляем на текущей позиции
                    _blocks[i].Orientation = Orientation.Vertical;
                }
                else
                {
                    // Последующие блоки располагаем относительно предыдущего
                    var previousBlock = _blocks[i - 1];
                    _blocks[i].Orientation = Orientation.Vertical;
                    _blocks[i].Position = new Point(
                        previousBlock.Position.X,
                        previousBlock.Position.Y + previousBlock.Height + spacing
                    );
                }
            }
            this.Invalidate();
        }

        public void ArrangeBlocksHorizontally()
        {
            int spacing = 20;

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (i == 0)
                {
                    // Первый блок оставляем на текущей позиции
                    _blocks[i].Orientation = Orientation.Horizontal;
                }
                else
                {
                    // Последующие блоки располагаем относительно предыдущего
                    var previousBlock = _blocks[i - 1];
                    _blocks[i].Orientation = Orientation.Horizontal;
                    _blocks[i].Position = new Point(
                        previousBlock.Position.X + previousBlock.Width + spacing,
                        previousBlock.Position.Y
                    );
                }
            }
            this.Invalidate();
        }


        public void UpdatePlayerData(int blockIndex, int subBlockIndex, string name, double damage, double percent, bool isConnected, int classType)
        {
            if (blockIndex >= 0 && blockIndex < _blocks.Count)
            {
                _blocks[blockIndex].UpdateSubBlock(subBlockIndex, name, damage, percent, isConnected, classType);
                this.Invalidate();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }

    public class PlayerBlock
    {
        public string[] PlayerNames { get; set; } = new string[4]; // Имена для 4 подблоков
        public double[] Damages { get; set; } = new double[4];     // Урон для 4 подблоков  
        public double[] Percentages { get; set; } = new double[4]; // Проценты для 4 подблоков
        public bool[] IsConnected { get; set; } = new bool[4];     // Статус для 4 подблоков
        public int[] ClassTypes { get; set; } = new int[4];        // Массив для типов классов

        public Point Position { get; set; }
        public Orientation Orientation { get; set; }
        public Color[] Colors { get; set; }
        public int BlockIndex { get; set; }

        // DPI scaling
        private float DpiScaleX = 1.0f;
        private float DpiScaleY = 1.0f;

        // Базовые размеры (для 96 DPI)
        private const int BaseElementWidth = 115;
        private const int BaseElementHeight = 30;
        private const int BaseSpacing = 2;
        private const int BaseFontSize = 8;

        // Масштабированные размеры
        public int ScaledElementWidth => (int)(BaseElementWidth * DpiScaleX);
        public int ScaledElementHeight => (int)(BaseElementHeight * DpiScaleY);
        public int ScaledSpacing => (int)(BaseSpacing * DpiScaleX);
        public float ScaledFontSize => BaseFontSize * DpiScaleY;

        public int Width => Orientation == Orientation.Horizontal ?
               ScaledElementWidth * 4 + ScaledSpacing * 3 : ScaledElementWidth;

        public int Height => Orientation == Orientation.Vertical ?
            ScaledElementHeight * 4 + ScaledSpacing * 3 : ScaledElementHeight;

        public Rectangle Bounds => new Rectangle(Position.X, Position.Y, Width, Height);

        // Обновляем данные конкретного подблока (0-3)
        public void UpdateSubBlock(int subBlockIndex, string name, double damage, double percent, bool isConnected, int classType)
        {
            if (subBlockIndex >= 0 && subBlockIndex < 4)
            {
                PlayerNames[subBlockIndex] = name;
                Damages[subBlockIndex] = damage;
                Percentages[subBlockIndex] = percent;
                IsConnected[subBlockIndex] = isConnected;
                ClassTypes[subBlockIndex] = classType; 
            }
        }

        private bool IsSupportPlayer(int subBlockIndex)
        {
            return subBlockIndex >= 0 && subBlockIndex < 4 && ClassTypes[subBlockIndex] == 0;
        }

        public PlayerBlock(string[] colors, int index, Graphics? g = null) 
        {
            // Получаем масштаб DPI
            if (g != null)
            {
                DpiScaleX = g.DpiX / 96.0f;
                DpiScaleY = g.DpiY / 96.0f;
            }

            // Инициализируем массивы
            PlayerNames = new string[4];
            Damages = new double[4];
            Percentages = new double[4];
            IsConnected = new bool[4];

            Colors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Colors[i] = ColorTranslator.FromHtml(colors[i]);
            }

            BlockIndex = index;

            // Масштабируем начальную позицию
            int baseY = 50 + index * 150;
            Position = new Point((int)(50 * DpiScaleX), (int)(baseY * DpiScaleY));
            Orientation = Orientation.Vertical;
        }

        public void Draw(Graphics g)
        {
            for (int i = 0; i < 4; i++)
            {
                Rectangle rect = GetElementRectangle(i);

                // Если игрок отключен, делаем цвета бледнее
                Color color;
                if (!IsConnected[i])
                {
                    // Для отключенных игроков - полупрозрачный серый поверх основного цвета
                    using (var brush = new SolidBrush(Colors[i]))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    // Поверх рисуем полупрозрачный серый overlay
                    using (var overlayBrush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
                    {
                        g.FillRectangle(overlayBrush, rect);
                    }
                    color = Colors[i]; // Основной цвет для рамки
                }
                else
                {
                    color = Colors[i];
                    using (var brush = new SolidBrush(color))
                    {
                        g.FillRectangle(brush, rect);
                    }
                }

                // Масштабируем толщину пера
                using (var pen = new Pen(Color.Black, 1 * DpiScaleX))
                {
                    g.DrawRectangle(pen, rect);
                }

                // Добавляем эффект объема
                using (var lightPen = new Pen(Color.FromArgb(100, Color.White), 1 * DpiScaleX))
                using (var darkPen = new Pen(Color.FromArgb(100, Color.Black), 1 * DpiScaleX))
                {
                    g.DrawLine(lightPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    g.DrawLine(lightPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                    g.DrawLine(darkPen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                    g.DrawLine(darkPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                }
            }

            // Рисуем текст для всех подблоков
            DrawText(g);
        }

        private Rectangle GetElementRectangle(int index)
        {
            if (Orientation == Orientation.Horizontal)
            {
                return new Rectangle(
                    Position.X + index * (ScaledElementWidth + ScaledSpacing),
                    Position.Y,
                    ScaledElementWidth,
                    ScaledElementHeight
                );
            }
            else
            {
                return new Rectangle(
                    Position.X,
                    Position.Y + index * (ScaledElementHeight + ScaledSpacing),
                    ScaledElementWidth,
                    ScaledElementHeight
                );
            }
        }

        private void DrawSupportCross(Graphics g, Rectangle damageRect)
        {
            // Масштабируем размеры креста
            int crossLengthHorizontal = (int)(14 * DpiScaleX);
            int crossLengthVertical = (int)(14 * DpiScaleY);
            int thickness = (int)(3 * DpiScaleX);
            int offsetX = (int)(8 * DpiScaleX);
            int offsetY = (int)(3 * DpiScaleY);

            int centerX = damageRect.Left + offsetX;
            int centerY = damageRect.Top + (damageRect.Height / 4) + offsetY;

            int halfHorizontal = crossLengthHorizontal / 2;
            int halfVertical = crossLengthVertical / 2;
            int halfThickness = thickness / 2;

            // Горизонтальная линия - специально удлиняем правую часть
            Rectangle horizontalLine = new Rectangle(
                centerX - halfHorizontal + 1, // Укорачиваем левую часть на 2px
                centerY - halfThickness,
                crossLengthHorizontal - 1,    // Компенсируем удлинение правой части
                thickness
            );

            // Вертикальная линия - специально удлиняем нижнюю часть
            Rectangle verticalLine = new Rectangle(
                centerX - halfThickness,
                centerY - halfVertical + 1,   // Укорачиваем верхнюю часть на 2px
                thickness,
                crossLengthVertical - 1       // Компенсируем удлинение нижней части
            );

            // 1. Чёрная обводка
            using (var blackBrush = new SolidBrush(Color.Black))
            {
                // Горизонтальная с обводкой (правая часть длиннее)
                g.FillRectangle(blackBrush,
                    horizontalLine.X - 1, horizontalLine.Y - 1,
                    horizontalLine.Width + 2, horizontalLine.Height + 1); // +3 справа

                // Вертикальная с обводкой (нижняя часть длиннее)
                g.FillRectangle(blackBrush,
                    verticalLine.X - 1, verticalLine.Y - 1,
                    verticalLine.Width + 1, verticalLine.Height + 2); // +3 снизу
            }

            // 2. Белая прослойка
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, horizontalLine);
                g.FillRectangle(whiteBrush, verticalLine);
            }

            // 3. Зелёный крест
            using (var greenBrush = new SolidBrush(Color.FromArgb(255, 76, 175, 80)))
            {
                // Немного уменьшаем для зелёного слоя
                var greenHorizontal = horizontalLine;
                greenHorizontal.Inflate((int)(-1 * DpiScaleX), (int)(-1 * DpiScaleY));
                var greenVertical = verticalLine;
                greenVertical.Inflate((int)(-1 * DpiScaleX), (int)(-1 * DpiScaleY));

                g.FillRectangle(greenBrush, greenHorizontal);
                g.FillRectangle(greenBrush, greenVertical);
            }
        }

        private void DrawText(Graphics g)
        {
            // Рисуем текст для каждого подблока
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrEmpty(PlayerNames[i])) continue;

                string nameText = PlayerNames[i];
                string damageText = $"{Damages[i]:F2} ({Percentages[i]:F2}%)";

                using (var font = new Font("Arial", ScaledFontSize, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var outlineBrush = new SolidBrush(Color.FromArgb(150, Color.Black)))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near
                    };

                    // Получаем прямоугольник текущего подблока
                    var rect = GetElementRectangle(i);
                    
                    // Масштабируем отступы
                    int padding = (int)(2 * DpiScaleX);
                    var textRect = new Rectangle(rect.X + padding, rect.Y + padding,
                                               rect.Width - padding * 2, rect.Height - padding * 2);

                    // Если игрок отключен, рисуем полупрозрачный overlay
                    if (!IsConnected[i])
                    {
                        using (var overlayBrush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
                        {
                            g.FillRectangle(overlayBrush, rect);
                        }
                    }

                    // Рисуем обводку для имени
                    var nameRect = textRect;
                    int outlineOffset = (int)(1 * DpiScaleX); // Масштабируем смещение обводки
                    for (int x = -outlineOffset; x <= outlineOffset; x++)
                    {
                        for (int y = -outlineOffset; y <= outlineOffset; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            var outlineRect = nameRect;
                            outlineRect.Offset(x, y);
                            g.DrawString(nameText, font, outlineBrush, outlineRect, format);
                        }
                    }

                    // Рисуем основное имя
                    g.DrawString(nameText, font, brush, nameRect, format);

                    // Рисуем обводку для урона (ниже имени)
                    //var damageRect = textRect;
                    //damageRect.Y += (int)(font.Height * 1.2);

                    int verticalSpacing = (int)(14 * DpiScaleY); // Фиксированный отступ 14px с масштабированием

                    var damageRect = new Rectangle(
                        textRect.X,
                        textRect.Y + verticalSpacing, // Вместо font.Height * 1.2
                        textRect.Width,
                        textRect.Height - verticalSpacing
                    );


                    int damageOffset = 0;
                    if (IsSupportPlayer(i))
                    {
                        damageOffset = (int)(16 * DpiScaleX); // Масштабируем смещение
                        DrawSupportCross(g, damageRect); // Передаем damageRect вместо textRect
                    }

                    var shiftedDamageRect = new Rectangle(
                        damageRect.X + damageOffset,
                        damageRect.Y,
                        damageRect.Width - damageOffset,
                        damageRect.Height
                    );

                    // Рисуем обводку для текста урона
                    for (int x = -outlineOffset; x <= outlineOffset; x++)
                    {
                        for (int y = -outlineOffset; y <= outlineOffset; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            //var outlineRect = damageRect;
                            var outlineRect = shiftedDamageRect;
                            outlineRect.Offset(x, y);
                            g.DrawString(damageText, font, outlineBrush, outlineRect, format);
                        }
                    }

                    // Рисуем основной текст урона
                    g.DrawString(damageText, font, brush, shiftedDamageRect, format);
                }
            }
        }
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public class BlockConfig
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Orientation Orientation { get; set; }
    }

}