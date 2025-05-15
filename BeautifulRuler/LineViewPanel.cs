using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace BeautifulRuler
{
    public class LineViewPanel : Panel
    {
        // 所有线条的集合
        private List<LineInfo> _lines = new List<LineInfo>();

        // 当前拖动的线段
        private LineInfo _draggingLine = null;
        private Point _dragStartPoint;
        private int _dragStartX;
        private Dictionary<LineInfo, List<ConnectionInfo>> _lineConnections = new Dictionary<LineInfo, List<ConnectionInfo>>();

        // 连接信息类
        private class ConnectionInfo
        {
            public LineInfo Line { get; set; }
            public bool ConnectedToStart { get; set; }
            public bool ConnectedToEnd { get; set; }
            public Point OriginalOtherEnd { get; set; }
            public bool IsLeftConnection => ConnectedToStart;
            public bool IsRightConnection => ConnectedToEnd;
        }

        // 线条信息类
        public class LineInfo
        {
            public Point StartPoint { get; set; }
            public Point EndPoint { get; set; }
            public string LabelText { get; set; }
            public string No { get; set; }
            public Color LineColor { get; set; }
            public int LineWidth { get; set; }
            public Point LabelOffset { get; set; } = new Point(0, -10);
            public bool IsDraggable { get; set; }

            // 计算是否是水平线
            public bool IsHorizontalLine => Math.Abs(EndPoint.Y - StartPoint.Y) < 5;

            // 计算线段中点
            public Point MidPoint => new Point(
                (StartPoint.X + EndPoint.X) / 2,
                (StartPoint.Y + EndPoint.Y) / 2);

            // 判断点是否在线段附近
            public bool IsPointNearLine(Point p, int tolerance)
            {
                // 如果是水平线，只检查X方向
                if (IsHorizontalLine)
                {
                    int minX = Math.Min(StartPoint.X, EndPoint.X) - tolerance;
                    int maxX = Math.Max(StartPoint.X, EndPoint.X) + tolerance;
                    int minY = StartPoint.Y - tolerance;
                    int maxY = StartPoint.Y + tolerance;

                    return p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY;
                }
                else
                {
                    // 计算点到线段的距离
                    double distance = Math.Abs((EndPoint.Y - StartPoint.Y) * p.X - (EndPoint.X - StartPoint.X) * p.Y + EndPoint.X * StartPoint.Y - EndPoint.Y * StartPoint.X)
                                    / Math.Sqrt(Math.Pow(EndPoint.Y - StartPoint.Y, 2) + Math.Pow(EndPoint.X - StartPoint.X, 2));
                    return distance <= tolerance;
                }
            }

            // 水平移动线段
            public void HorizontalMove(int deltaX)
            {
                StartPoint = new Point(StartPoint.X + deltaX, StartPoint.Y);
                EndPoint = new Point(EndPoint.X + deltaX, EndPoint.Y);
            }
        }

        public LineViewPanel()
        {
            // 启用双缓冲防止闪烁
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            // 设置为透明背景
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;

            // 启用鼠标事件
            this.MouseDown += LineViewPanel_MouseDown;
            this.MouseMove += LineViewPanel_MouseMove;
            this.MouseUp += LineViewPanel_MouseUp;
        }

        // 清除所有线条
        public void ClearLines()
        {
            _lines.Clear();
            _lineConnections.Clear();
            Invalidate();
        }

        // 添加线条
        public void AddLine(Point start, Point end, string text = "", string no = "")
        {
            var lineInfo = new LineInfo
            {
                StartPoint = start,
                EndPoint = end,
                LabelText = text,
                No = no,
                LineColor = string.IsNullOrEmpty(text) ? Color.Blue : Color.MediumBlue,
                LineWidth = string.IsNullOrEmpty(text) ? 1 : 3,
                IsDraggable = !string.IsNullOrEmpty(text)
            };

            _lines.Add(lineInfo);
            _lineConnections[lineInfo] = new List<ConnectionInfo>();
            Invalidate();

            // 找到与当前添加的线相连的连接线
            if (!string.IsNullOrEmpty(text))
            {
                FindConnectedLines(lineInfo);
            }
        }

        // 查找与指定线段关联的所有连接线
        private void FindConnectedLines(LineInfo mainLine)
        {
            _lineConnections[mainLine].Clear();

            // 获取所有连接线（没有标签的线）
            var connectionLines = _lines.Where(l => string.IsNullOrEmpty(l.LabelText)).ToList();

            foreach (var line in connectionLines)
            {
                // 检查连接线的起点是否连接到当前线段的终点
                if (IsPointsClose(line.StartPoint, mainLine.EndPoint, 10))
                {
                    _lineConnections[mainLine].Add(new ConnectionInfo
                    {
                        Line = line,
                        ConnectedToEnd = true,
                        OriginalOtherEnd = line.EndPoint
                    });
                }
                // 检查连接线的终点是否连接到当前线段的起点
                else if (IsPointsClose(line.EndPoint, mainLine.StartPoint, 10))
                {
                    _lineConnections[mainLine].Add(new ConnectionInfo
                    {
                        Line = line,
                        ConnectedToStart = true,
                        OriginalOtherEnd = line.StartPoint
                    });
                }
            }
        }

        // 判断两个点是否接近
        private bool IsPointsClose(Point p1, Point p2, int tolerance)
        {
            return Math.Abs(p1.X - p2.X) <= tolerance && Math.Abs(p1.Y - p2.Y) <= tolerance;
        }

        // 水平移动所有线条
        public void HorizontalShift(int pixelDiff)
        {
            foreach (var line in _lines)
            {
                line.StartPoint = new Point(line.StartPoint.X + pixelDiff, line.StartPoint.Y);
                line.EndPoint = new Point(line.EndPoint.X + pixelDiff, line.EndPoint.Y);
            }
            Invalidate();
        }

        // 鼠标按下事件
        private void LineViewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            _draggingLine = null;

            // 查找鼠标下方的可拖动线段
            foreach (var line in _lines.Where(l => l.IsDraggable))
            {
                if (line.IsPointNearLine(e.Location, 8))
                {
                    _draggingLine = line;
                    _dragStartPoint = e.Location;
                    _dragStartX = e.X;
                    Cursor = Cursors.Hand;

                    // 打印拖动开始前的坐标
                    Console.WriteLine($"拖动开始 - 线条: {line.LabelText ?? "未命名"} (编号: {line.No ?? "无"})");
                    Console.WriteLine($"起始坐标: ({line.StartPoint.X}, {line.StartPoint.Y})");
                    Console.WriteLine($"结束坐标: ({line.EndPoint.X}, {line.EndPoint.Y})");

                    // 重新查找连接线，确保连接信息是最新的
                    FindConnectedLines(_draggingLine);
                    break;
                }
            }
        }

        // 鼠标移动事件
        private void LineViewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingLine != null)
            {
                // 计算水平移动距离
                int deltaX = e.X - _dragStartPoint.X;

                // 应用移动
                _draggingLine.HorizontalMove(deltaX);

                // 更新关联的连接线
                UpdateConnectedLines(_draggingLine);

                // 更新拖动起点
                _dragStartPoint = e.Location;

                Invalidate();
            }
        }

        // 鼠标松开事件
        private void LineViewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_draggingLine != null)
            {
                // 打印拖动结束后的坐标
                Console.WriteLine($"拖动结束 - 线条: {_draggingLine.LabelText ?? "未命名"} (编号: {_draggingLine.No ?? "无"})");
                Console.WriteLine($"起始坐标: ({_draggingLine.StartPoint.X}, {_draggingLine.StartPoint.Y})");
                Console.WriteLine($"结束坐标: ({_draggingLine.EndPoint.X}, {_draggingLine.EndPoint.Y})");

                _draggingLine = null;
                Cursor = Cursors.Default;
            }
        }

        // 更新与指定线段关联的所有连接线
        private void UpdateConnectedLines(LineInfo mainLine)
        {
            if (!_lineConnections.ContainsKey(mainLine))
                return;

            foreach (var connInfo in _lineConnections[mainLine])
            {
                var line = connInfo.Line;

                // 处理连接线的起点连接到当前线段的终点的情况
                if (connInfo.ConnectedToEnd)
                {
                    // 新起点是当前线段的终点
                    line.StartPoint = mainLine.EndPoint;
                }
                // 处理连接线的终点连接到当前线段的起点的情况
                else if (connInfo.ConnectedToStart)
                {
                    // 新终点是当前线段的起点
                    line.EndPoint = mainLine.StartPoint;
                }
            }
        }

        // 绘制所有线条
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 先绘制所有斜线（底层）
            var diagonalLines = _lines.Where(line => !line.IsHorizontalLine).ToList();
            foreach (var line in diagonalLines)
            {
                DrawLine(g, line, true);
            }

            // 再绘制所有水平线（顶层）
            var horizontalLines = _lines.Where(line => line.IsHorizontalLine).ToList();
            foreach (var line in horizontalLines)
            {
                DrawLine(g, line, false);
            }
        }

        // 绘制单条线
        private void DrawLine(Graphics g, LineInfo line, bool isDiagonal)
        {
            // 设置线条颜色和宽度
            Color lineColor = line.LineColor;
            int lineWidth = line.LineWidth;

            // 如果是斜线，则用半透明色
            if (isDiagonal)
            {
                lineColor = Color.FromArgb(180, 0, 0, 255); // 半透明蓝色
                lineWidth = 1;
            }

            using (Pen pen = new Pen(lineColor, lineWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, line.StartPoint, line.EndPoint);
            }

            // 绘制端点标记（仅对水平线或有标签的线）
            if (!isDiagonal || !string.IsNullOrEmpty(line.LabelText))
            {
                DrawPointMarker(g, line.StartPoint);
                DrawPointMarker(g, line.EndPoint);
            }

            // 绘制标签文本
            if (!string.IsNullOrEmpty(line.LabelText))
            {
                Point midPoint = line.MidPoint;
                Point labelPos = new Point(
                    midPoint.X + line.LabelOffset.X,
                    midPoint.Y + line.LabelOffset.Y);

                Size textSize = TextRenderer.MeasureText(line.LabelText, this.Font);
                Point drawPoint = new Point(
                    labelPos.X - textSize.Width / 2,
                    labelPos.Y - textSize.Height / 2);

                // 绘制白色半透明背景
                Rectangle textRect = new Rectangle(
                    drawPoint.X - 2, drawPoint.Y - 2,
                    textSize.Width + 4, textSize.Height + 4);
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(220, Color.White)))
                {
                    g.FillRectangle(bgBrush, textRect);
                }

                TextRenderer.DrawText(g, line.LabelText, this.Font, drawPoint, Color.Black);
            }

            // 绘制编号
            if (!string.IsNullOrEmpty(line.No))
            {
                Point midPoint = line.MidPoint;
                Point noPos = new Point(
                    midPoint.X + line.LabelOffset.X,
                    midPoint.Y + line.LabelOffset.Y + 20);

                Size textSize = TextRenderer.MeasureText(line.No, this.Font);
                Point drawPoint = new Point(
                    noPos.X - textSize.Width / 2,
                    noPos.Y - textSize.Height / 2);

                // 绘制白色半透明背景
                Rectangle textRect = new Rectangle(
                    drawPoint.X - 2, drawPoint.Y - 2,
                    textSize.Width + 4, textSize.Height + 4);
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(220, Color.White)))
                {
                    g.FillRectangle(bgBrush, textRect);
                }

                TextRenderer.DrawText(g, line.No, this.Font, drawPoint, Color.Black);
            }
        }

        // 绘制端点标记
        private void DrawPointMarker(Graphics g, Point p)
        {
            int markerSize = 6;
            using (Brush brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush,
                    p.X - markerSize / 2,
                    p.Y - markerSize / 2,
                    markerSize,
                    markerSize);
            }
        }
    }
}