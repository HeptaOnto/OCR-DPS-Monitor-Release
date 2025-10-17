using System;
using System.Drawing;
using System.Windows.Forms;

public class OverlayForm : Form
{
    private Point startPoint;
    private Rectangle selection;
    public Rectangle SelectedRegion => selection;

    public OverlayForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.Black;
        this.Opacity = 0.3;
        this.Cursor = Cursors.Cross;
        this.DoubleBuffered = true;
        this.TopMost = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        startPoint = e.Location;
        selection = new Rectangle(e.Location, new Size(0, 0));
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            selection = new Rectangle(
                Math.Min(startPoint.X, e.X),
                Math.Min(startPoint.Y, e.Y),
                Math.Abs(e.X - startPoint.X),
                Math.Abs(e.Y - startPoint.Y)
            );
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (selection.Width > 0 && selection.Height > 0)
        {
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, selection);
            }
        }
    }
}