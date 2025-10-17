using System;
using System.Drawing;
using System.Windows.Forms;

namespace OCR_DPS_Monitor
{
    public class OverlayForm : Form
    {
        private bool isSelecting = false;
        private Point startPoint;
        private Rectangle selection = Rectangle.Empty;

        public Rectangle SelectedRegion { get; private set; }
        private const int DARKEN_ALPHA = 120;

        // Используем специальный цвет для прозрачности, а не черный
        private readonly Color transparentColor = Color.DarkGray;

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;
            this.ShowInTaskbar = false;

            // Настройка прозрачности - используем специальный цвет
            this.BackColor = transparentColor;
            this.TransparencyKey = transparentColor;
            this.Opacity = 0.5; 
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selection = new Rectangle(e.Location, Size.Empty);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                selection = new Rectangle(
                    Math.Min(startPoint.X, e.X),
                    Math.Min(startPoint.Y, e.Y),
                    Math.Abs(e.X - startPoint.X),
                    Math.Abs(e.Y - startPoint.Y));
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                SelectedRegion = selection;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle screenBounds = this.ClientRectangle;

            // Сначала очищаем все прозрачным цветом
            g.Clear(transparentColor);

            // Затем затемняем всю область экрана черным с прозрачностью
            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(DARKEN_ALPHA, Color.Black)))
            {
                g.FillRectangle(darkBrush, screenBounds);
            }

            if (selection.Width > 0 && selection.Height > 0)
            {
                // Делаем выделенную область прозрачной (убираем затемнение)
                g.SetClip(selection);
                g.Clear(transparentColor); // Используем специальный цвет для прозрачности
                g.ResetClip();

                // Рисуем красную рамку вокруг выделенной области
                using (Pen borderPen = new Pen(Color.Red, 2))
                {
                    g.DrawRectangle(borderPen, selection);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            base.OnKeyDown(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            base.OnMouseClick(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                return cp;
            }
        }
    }
}