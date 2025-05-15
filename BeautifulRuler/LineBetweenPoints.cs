using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeautifulRuler
{
    /// <summary>
    /// 线条控件类，已被 LineViewPanel 替代。
    /// </summary>
    [Obsolete("此类已过时，请使用 LineViewPanel 替代。LineViewPanel 提供更好的绘图控制和性能。")]
    public partial class LineControl : Control
    {
        // 定义两个点的坐标
        public Point _pointA = new Point(300, 50);
        public Point _pointB = new Point(200, 50);

        // 线条样式
        private Color _lineColor = Color.Blue;
        private int _lineWidth = 5;

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
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // 点击时移动第一个点
            //PointA = e.Location;
        }
    }
}