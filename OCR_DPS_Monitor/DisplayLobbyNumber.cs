using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OCR_DPS_Monitor
{
    public partial class DisplayLobbyNumber : Form
    {
        private string number;
        private float opacity = 1.0f;
        private System.Windows.Forms.Timer fadeTimer;

        private const int fadeDuration = 2500;
        private int elapsedTime = 0;


        public DisplayLobbyNumber(string displayNumber)
        {
            this.number = displayNumber;

            InitializeForm();
            InitializeTimer();
            InitializeComponent();
        }

        private void InitializeForm()
        {

            // Настройки формы
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true; // Поверх всех окон
            this.BackColor = Color.Black;
            //this.Opacity = 1.0;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            // Прозрачность фона (click-through)
            this.TransparencyKey = Color.Black;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Добавляем стиль WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x80;

                // WS_EX_TRANSPARENT - click-through (клики проходят сквозь)
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT

                // WS_EX_LAYERED - для прозрачности
                cp.ExStyle |= 0x80000; // WS_EX_LAYERED

                // WS_EX_NOACTIVATE - нельзя активировать
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private void InitializeTimer()
        {
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 16; // ~60 FPS
            fadeTimer.Tick += FadeTimer_Tick;
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            elapsedTime += fadeTimer.Interval;

            // Вычисляем новую прозрачность
            opacity = 1.0f - ((float)elapsedTime / fadeDuration);

            if (opacity <= 0)
            {
                opacity = 0;
                fadeTimer.Stop();
                this.Close();
                return;
            }

            this.Invalidate(); // Принудительная перерисовка
        }

       
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Текст от белого (альфа=255) до прозрачного (альфа=0)
            int alpha = (int)(opacity * 255);

            if (alpha > 50)
            {
                // Создаем кисть с текущей прозрачностью
                using (var brush = new SolidBrush(Color.FromArgb(alpha, Color.White))) //((int)(opacity * 255)
                using (var font = new Font("Segoe UI", 72, FontStyle.Bold, GraphicsUnit.Point))
                {
                    // Вычисляем размер текста для центрирования
                    var textSize = g.MeasureString(number, font);
                    var x = (this.Width - textSize.Width) / 2;
                    var y = (this.Height - textSize.Height) / 2;

                    // Рисуем номер
                    g.DrawString(number, font, brush, x, y);
                }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            fadeTimer.Start();
        }
    }
}
