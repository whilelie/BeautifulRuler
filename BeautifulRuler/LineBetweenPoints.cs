using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

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

        // 存储与此线段关联的连接线
        private List<ConnectionInfo> _connectedLines = new List<ConnectionInfo>();

        // 存储连接信息的类
        private class ConnectionInfo
        {
            public LineControl Line { get; set; }
            public bool ConnectedToStart { get; set; } // 是否连接到当前线段的起点
            public bool ConnectedToEnd { get; set; } // 是否连接到当前线段的终点
            public Point OriginalOtherEnd { get; set; } // 连接线另一端的原始坐标（绝对坐标）
        }

        public LineControl()
        {
            DoubleBuffered = true; // 启用双缓冲防止闪烁
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
            if (!string.IsNullOrEmpty(LabelText))
            {
                _lineColor = Color.MediumBlue;
                _lineWidth = 2;
            }
            else
            {
                _lineColor = Color.Blue;
                _lineWidth = 1;
            }
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

            // 在线段下绘制编号
            if (!string.IsNullOrEmpty(No))
            {
                // 计算线段中点
                int midX = (_pointA.X + _pointB.X) / 2;
                int midY = (_pointA.Y + _pointB.Y) / 2;

                // 计算编号位置：在线段中点下方 20 像素（可根据需要调整）
                int offsetY = 20;
                var noPos = new Point(midX + LabelOffset.X, midY + LabelOffset.Y + offsetY);

                Size textSize = TextRenderer.MeasureText(No, this.Font);
                var drawPoint = new Point(noPos.X - textSize.Width / 2, noPos.Y - textSize.Height / 2);

                TextRenderer.DrawText(g, No, this.Font, drawPoint, Color.Black, TextFormatFlags.Default);
            }
        }

        // 绘制圆形端点标记
        private void DrawPointMarker(Graphics g, Point p)
        {
            int markerSize = 4;
            using (Brush brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush,
                    p.X - markerSize / 2,
                    p.Y - markerSize / 2,
                    markerSize,
                    markerSize);
            }
        }

        private bool _dragging = false;
        private Point _dragStart;
        private Point _controlStart;
        private int _originalY; // 保存原始Y坐标

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 判断是否是非连接线，且在线段附近
            if (!string.IsNullOrEmpty(LabelText) && IsPointNearLine(e.Location, _pointA, _pointB, 8))
            {
                _dragging = true;
                _dragStart = e.Location;
                _controlStart = this.Location;
                _originalY = this.Location.Y; // 记录开始拖动时的Y坐标
                Cursor = Cursors.Hand;

                // 查找与此线关联的所有连接线
                FindConnectedLines();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                // 计算水平方向的位移
                int xOffset = e.Location.X - _dragStart.X;

                // 只移动X坐标，保持Y坐标不变
                this.Location = new Point(_controlStart.X + xOffset, _originalY);

                // 更新所有关联的连接线
                UpdateConnectedLines(xOffset);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Cursor = Cursors.Default;

                // 清除关联的连接线列表
                _connectedLines.Clear();
            }
        }

        // 查找与当前线段关联的所有连接线
        private void FindConnectedLines()
        {
            _connectedLines.Clear();

            // 获取父容器中的所有LineControl
            if (Parent != null)
            {
                var allLines = Parent.Controls.OfType<LineControl>().ToList();

                // 获取当前线段两端在容器坐标系中的绝对位置
                Point absPointA = PointToScreen(_pointA);
                Point absPointB = PointToScreen(_pointB);
                absPointA = Parent.PointToClient(absPointA);
                absPointB = Parent.PointToClient(absPointB);

                // 查找连接到当前线段的连接线（LabelText为空的线）
                foreach (var line in allLines)
                {
                    if (line != this && string.IsNullOrEmpty(line.LabelText))
                    {
                        // 获取连接线两端在容器坐标系中的绝对位置
                        Point lineAbsPointA = line.PointToScreen(line._pointA);
                        Point lineAbsPointB = line.PointToScreen(line._pointB);
                        lineAbsPointA = Parent.PointToClient(lineAbsPointA);
                        lineAbsPointB = Parent.PointToClient(lineAbsPointB);

                        var connectionInfo = new ConnectionInfo { Line = line };

                        // 检查连接线的起点是否连接到当前线段的终点
                        if (IsPointsClose(lineAbsPointA, absPointB, 10))
                        {
                            connectionInfo.ConnectedToEnd = true;
                            connectionInfo.OriginalOtherEnd = lineAbsPointB;
                            _connectedLines.Add(connectionInfo);
                        }
                        // 检查连接线的终点是否连接到当前线段的起点
                        else if (IsPointsClose(lineAbsPointB, absPointA, 10))
                        {
                            connectionInfo.ConnectedToStart = true;
                            connectionInfo.OriginalOtherEnd = lineAbsPointA;
                            _connectedLines.Add(connectionInfo);
                        }
                    }
                }
            }
        }

        // 更新与当前线段关联的所有连接线
        private void UpdateConnectedLines(int xOffset)
        {
            foreach (var connInfo in _connectedLines)
            {
                var line = connInfo.Line;

                // 获取当前线段移动后的端点绝对位置
                Point absPointA = PointToScreen(_pointA);
                Point absPointB = PointToScreen(_pointB);
                absPointA = Parent.PointToClient(absPointA);
                absPointB = Parent.PointToClient(absPointB);

                // 处理连接线的起点连接到当前线段的终点的情况
                if (connInfo.ConnectedToEnd)
                {
                    // 计算连接线新的尺寸和位置
                    Point newStartPoint = absPointB; // 新起点是当前线段的终点
                    Point newEndPoint = connInfo.OriginalOtherEnd; // 终点保持不变

                    // 创建新线段的最小包围矩形
                    int minX = Math.Min(newStartPoint.X, newEndPoint.X);
                    int minY = Math.Min(newStartPoint.Y, newEndPoint.Y);
                    int maxX = Math.Max(newStartPoint.X, newEndPoint.X);
                    int maxY = Math.Max(newStartPoint.Y, newEndPoint.Y);

                    // 计算新的控件大小和位置
                    int width = maxX - minX + 5;
                    int height = maxY - minY + 50;
                    width = Math.Max(width, 1);
                    height = Math.Max(height, 1);

                    // 更新连接线控件
                    line.Location = new Point(minX - 5, minY - 25);
                    line.Size = new Size(width, height);

                    // 更新连接线端点在控件内的相对坐标
                    line._pointA = new Point(newStartPoint.X - (minX - 5), newStartPoint.Y - (minY - 25));
                    line._pointB = new Point(newEndPoint.X - (minX - 5), newEndPoint.Y - (minY - 25));
                    line.Invalidate();
                }
                // 处理连接线的终点连接到当前线段的起点的情况
                else if (connInfo.ConnectedToStart)
                {
                    // 计算连接线新的尺寸和位置
                    Point newStartPoint = connInfo.OriginalOtherEnd; // 起点保持不变
                    Point newEndPoint = absPointA; // 新终点是当前线段的起点

                    // 创建新线段的最小包围矩形
                    int minX = Math.Min(newStartPoint.X, newEndPoint.X);
                    int minY = Math.Min(newStartPoint.Y, newEndPoint.Y);
                    int maxX = Math.Max(newStartPoint.X, newEndPoint.X);
                    int maxY = Math.Max(newStartPoint.Y, newEndPoint.Y);

                    // 计算新的控件大小和位置
                    int width = maxX - minX + 5;
                    int height = maxY - minY + 50;
                    width = Math.Max(width, 1);
                    height = Math.Max(height, 1);

                    // 更新连接线控件
                    line.Location = new Point(minX - 5, minY - 25);
                    line.Size = new Size(width, height);

                    // 更新连接线端点在控件内的相对坐标
                    line._pointA = new Point(newStartPoint.X - (minX - 5), newStartPoint.Y - (minY - 25));
                    line._pointB = new Point(newEndPoint.X - (minX - 5), newEndPoint.Y - (minY - 25));
                    line.Invalidate();
                }
            }
        }

        // 判断两个点是否接近
        private bool IsPointsClose(Point p1, Point p2, int tolerance)
        {
            return Math.Abs(p1.X - p2.X) <= tolerance && Math.Abs(p1.Y - p2.Y) <= tolerance;
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
        public string No { get; set; }
    }
}