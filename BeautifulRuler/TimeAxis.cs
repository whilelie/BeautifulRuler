using System;
using System.Drawing;
using System.Windows.Forms;

public class TimeAxis : Panel
{
    // 字段和属性
    private DateTime _baseStartTime = DateTime.Today;
    private int _offsetHours;
    private Font _timeFont = new Font("Microsoft YaHei", 8);
    private Font _dateFont = new Font("Microsoft YaHei", 8, FontStyle.Bold);
    private Color _axisColor = Color.Black;
    private Timer _refreshTimer;

    public int xr_i;
    // 公共属性
    public float PixelsPerHour { get; set; } = 50;
    public Color CurrentTimeColor { get; set; } = Color.Red;
    public float CurrentTimeLineWidth { get; set; } = 2f;

    // 时间范围计算属性
    public DateTime VisibleStartTime => _baseStartTime.AddHours(_offsetHours);
    public DateTime VisibleEndTime => _baseStartTime.AddHours(_offsetHours + TotalVisibleHours);
    public int TotalVisibleHours => (int)Math.Ceiling((double)ClientSize.Width / PixelsPerHour);

    // 构造函数
    public TimeAxis()
    {
        DoubleBuffered = true;
        Size = new Size(800, 100);
        BackColor = Color.White;
        InitializeTimer();
    }

    // 时间轴滚动方法
    public void MoveForward(int hours = 1)
    {
        _offsetHours += hours;
        Invalidate();
    }

    public void MoveBackward(int hours = 1)
    {
        _offsetHours -= hours;
        Invalidate();
    }



    private void InitializeTimer()
    {
        _refreshTimer = new Timer { Interval = 1000 }; // 每秒刷新
        _refreshTimer.Tick += (s, e) => Invalidate();
        _refreshTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        DrawTimeAxis(e.Graphics);
        DrawCurrentTimeLine(e.Graphics);
    }
    // 刻度表上线高度
    private void DrawTick(Graphics g, float x, float axisY)
    {
        using (Pen tickPen = new Pen(_axisColor, 1))
        {
            g.DrawLine(tickPen, x, axisY , x, axisY + 10);
        }
    }
    // 新增手动居中方法
    public int CenterCurrentTime(  )
    {
        DateTime now = DateTime.Now;
        TimeSpan span = now - _baseStartTime;
        float targetOffset = (float)(span.TotalHours - (ClientSize.Width / (2 * PixelsPerHour)));
        _offsetHours = Convert.ToInt32( Math.Max(0, targetOffset));
        Invalidate();
        return _offsetHours;
    }
    // 补全缺失的 DrawTimeLabel 方法
    private void DrawTimeLabel(Graphics g, string text, float x, float y)
    {
        using (var format = new StringFormat { Alignment = StringAlignment.Center })
        {
            SizeF textSize = g.MeasureString(text, _timeFont);
            RectangleF textRect = new RectangleF(
                x - textSize.Width / 2,
                y,
                textSize.Width,
                textSize.Height);
        
            g.DrawString(text, _timeFont, Brushes.Black, textRect, format);
        }
    }
    
    // 补全缺失的 DrawDateLabel 方法
    private void DrawDateLabel(Graphics g, string text, float x, float y)
    {
        using (var format = new StringFormat { Alignment = StringAlignment.Center })
        {
            SizeF textSize = g.MeasureString(text, _dateFont);
            RectangleF textRect = new RectangleF(
                x - textSize.Width / 2,
                y,
                textSize.Width,
                textSize.Height);

            g.DrawString(text, _dateFont, Brushes.DarkBlue, textRect, format);
        }
    }

    private void DrawTimeAxis(Graphics g)
    {
        float axisY = Height / 2;
        
        using (Pen axisPen = new Pen(_axisColor, 2))
        {
            g.DrawLine(axisPen, 0, axisY, Width, axisY);
        }

        // 在第一个刻度00:00到01:00之间绘制“工序”
        float x0 = GetPosition(VisibleStartTime);
        float x1 = GetPosition(VisibleStartTime.AddHours(1));
        float xMid = (x0 + x1) / 2;
        DrawTimeLabel(g, "工序", xMid, axisY - 20);

        DateTime current = VisibleStartTime;
        DateTime end = VisibleEndTime;

        while (current < end)
        {
            float x = GetPosition(current);
            
            DrawTick(g, x, axisY);
            DrawTimeLabel(g, current.ToString("HH:mm"), x, axisY + 5);
            
            
                if ((current.Hour == 0 || current.Hour == 8 || current.Hour == 16) )
            {
                DrawDateLabel(g, current.ToString("MM/dd"), x+20, axisY - 25);// 日期后移
            }

            current = current.AddHours(1);
        }
    }
    
    private void DrawCurrentTimeLine(Graphics g)
    {
        DateTime now = DateTime.Now;
        if (now < VisibleStartTime || now > VisibleEndTime) return;

        float x = GetPosition(now);
        using (Pen markerPen = new Pen(CurrentTimeColor, CurrentTimeLineWidth))
        {
            g.DrawLine(markerPen, x, 0, x, Height);

        }

        // 绘制时间标签
        string timeText = now.ToString("HH:mm:ss");
        using (var format = new StringFormat { Alignment = StringAlignment.Center })
        using (var font = new Font(_timeFont.FontFamily, 8, FontStyle.Bold))
        {
            RectangleF textRect = new RectangleF( x - 40,  Height - 50, 80,   20);
            
            using (var brush = new SolidBrush(CurrentTimeColor))
            {
                g.DrawString(timeText, font, brush, textRect, format);
            }
        }
    }

    public float GetPosition(DateTime time)
    {
        return (float)(time - VisibleStartTime).TotalHours * PixelsPerHour;
    }



    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Dispose();
            _timeFont?.Dispose();
            _dateFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}