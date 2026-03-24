using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class CircularProgressRing : UserControl
{
    private int _value = 0;
    private int _maximum = 100;
    private Color _progressColor = Color.LimeGreen;
    private Color _backColorRing = Color.FromArgb(80, 80, 80);
    private int _ringWidth = 12;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Min(value, _maximum);
            Invalidate(); // 触发重绘
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            Invalidate();
        }
    }

    public Color ProgressColor
    {
        get => _progressColor;
        set
        {
            _progressColor = value;
            Invalidate();
        }
    }

    public int RingWidth
    {
        get => _ringWidth;
        set
        {
            _ringWidth = value;
            Invalidate();
        }
    }

    public CircularProgressRing()
    {
        this.DoubleBuffered = true; // 减少闪烁
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        this.Size = new Size(100, 100);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        int width = this.Width;
        int height = this.Height;
        int size = Math.Min(width, height);
        Rectangle rect = new Rectangle(_ringWidth / 2, _ringWidth / 2, size - _ringWidth, size - _ringWidth);

        // 绘制背景圆环
        using (Pen backPen = new Pen(_backColorRing, _ringWidth))
        {
            backPen.StartCap = LineCap.Round;
            backPen.EndCap = LineCap.Round;
            e.Graphics.DrawEllipse(backPen, rect);
        }

        // 计算进度弧线角度
        float angle = 360f * _value / _maximum;

        // 绘制进度圆环（带阴影效果，可选）
        using (Pen progressPen = new Pen(_progressColor, _ringWidth))
        {
            progressPen.StartCap = LineCap.Round;
            progressPen.EndCap = LineCap.Round;
            // 先绘制阴影（偏移一点，透明度）
            using (Pen shadowPen = new Pen(Color.FromArgb(50, 0, 0, 0), _ringWidth))
            {
                shadowPen.StartCap = LineCap.Round;
                shadowPen.EndCap = LineCap.Round;
                e.Graphics.DrawArc(shadowPen, rect, -90, angle);
            }
            // 绘制实际进度
            e.Graphics.DrawArc(progressPen, rect, -90, angle);
        }

        // 绘制中心文本
        string text = $"{_value}%";
        using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
        {
            SizeF textSize = e.Graphics.MeasureString(text, font);
            float x = (width - textSize.Width) / 2;
            float y = (height - textSize.Height) / 2;

            // 文本阴影
            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                e.Graphics.DrawString(text, font, shadowBrush, x + 1, y + 1);
            }
            // 主文本
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(text, font, textBrush, x, y);
            }
        }
    }
}