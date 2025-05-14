using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeautifulRuler
{
    public partial class LineControl : Control
    {
        // 定义两个点的坐标
        public Point _pointA = new Point(300, 50);
        public Point _pointB = new Point(200, 50);

        // 线条样式
        private Color _lineColor = Color.Blue;
        private int _lineWidth = 2;

        public LineControl()
        {
            DoubleBuffered = true; // 启用双缓冲防止闪烁
            //Size = new Size(300, 200);
        }

        // 属性：可以通过属性修改坐标
        public Point PointA
        {
            get => _pointA;
            set { _pointA = value; Invalidate(); } // 修改后重绘
        }

        public Point PointB
        {
            get => _pointB;
            set { _pointB = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 绘制连接线
            using (Pen pen = new Pen(_lineColor, _lineWidth))
            {
                g.DrawLine(pen, _pointA, _pointB);
            }

            // 可选：绘制端点标记
                DrawPointMarker(g, _pointA);
                DrawPointMarker(g, _pointB);

            // 居中绘制标签
            if (!string.IsNullOrEmpty(LabelText))
            {
                int midX = (_pointA.X + _pointB.X) / 2;
                int midY = (_pointA.Y + _pointB.Y) / 2;
                var labelPos = new Point(midX + LabelOffset.X, midY + LabelOffset.Y);
                Size textSize = TextRenderer.MeasureText(LabelText, this.Font);
                var drawPoint = new Point(labelPos.X - textSize.Width / 2, labelPos.Y - textSize.Height / 2);
                TextRenderer.DrawText(g, LabelText, this.Font, drawPoint, Color.Black, TextFormatFlags.Default);
            }
        }

        // 绘制圆形端点标记
        private void DrawPointMarker(Graphics g, Point p)
        {
            int markerSize = 8;
            using (Brush brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush, 
                    p.X - markerSize/2, 
                    p.Y - markerSize/2, 
                    markerSize, 
                    markerSize);
            }
        }

        // 动态修改坐标示例（可通过鼠标事件触发）
        //protected override void OnMouseDown(MouseEventArgs e)
        //{
        //    base.OnMouseDown(e);
        //    // 点击时移动第一个点
        //    //PointA = e.Location;
        //}

        private bool _dragging = false;
        private Point _dragStart;
        private Point _controlStart;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // 判断是否点中线段
            if (IsPointNearLine(e.Location, _pointA, _pointB, 8))
            {
                _dragging = true;
                _dragStart = e.Location;
                _controlStart = this.Location;
                Cursor = Cursors.Hand;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                var offset = new Size(e.Location.X - _dragStart.X, e.Location.Y - _dragStart.Y);
                this.Location = new Point(_controlStart.X + offset.Width, _controlStart.Y + offset.Height);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Cursor = Cursors.Default;
            }
        }
        // 判断点是否在直线附近
        private bool IsPointNearLine(Point p, Point a, Point b, int tolerance)
        {
            double distance = Math.Abs((b.Y - a.Y) * p.X - (b.X - a.X) * p.Y + b.X * a.Y - b.Y * a.X)
                              / Math.Sqrt(Math.Pow(b.Y - a.Y, 2) + Math.Pow(b.X - a.X, 2));
            return distance <= tolerance;
        }

        // 在LineControl类中添加此方法
        public void EnableTransparentBackground()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
        }


        public string LabelText { get; set; }
        public Point LabelOffset { get; set; } = new Point(0, -30); // 默认在起点上方
    }
}