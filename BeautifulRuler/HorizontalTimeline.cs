using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BeautifulRuler
{
    public partial class TimelineControl : UserControl
    {
        // 时间轴节点数据（时间+描述）
        private List<TimelineEvent> _events = new List<TimelineEvent>();
        
        // 样式配置
        private readonly Color _lineColor = Color.Gray;
        private readonly Color _nodeColor = Color.SteelBlue;
        private readonly Color _textColor = Color.Black;
        private readonly int _lineHeight = 3;
        private readonly int _nodeRadius = 8;
        private readonly Font _textFont = new Font("Arial", 8);

        public List<TimelineEvent> Events
        {
            get => _events;
            set { _events = value; Invalidate(); } // 数据更新时重绘
        }

        public TimelineControl()
        {
            //InitializeComponent();
            DoubleBuffered = true; // 启用双缓冲防止闪烁
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_events.Count < 1) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 计算时间轴位置
            int timelineY = Height / 2;
            int startX = 50;  // 左边距
            int endX = Width - 50; // 右边距
            int totalWidth = endX - startX;

            // 绘制主线
            using (var pen = new Pen(_lineColor, _lineHeight))
            {
                g.DrawLine(pen, startX, timelineY, endX, timelineY);
            }

            // 绘制节点和标签
            for (int i = 0; i < _events.Count; i++)
            {
                // 计算节点位置（均匀分布）
                float ratio = (float)i / (_events.Count - 1);
                int nodeX = startX + (int)(totalWidth * ratio);

                // 绘制节点
                Rectangle nodeRect = new Rectangle(
                    nodeX - _nodeRadius,
                    timelineY - _nodeRadius,
                    _nodeRadius * 2,
                    _nodeRadius * 2
                );
                using (var brush = new SolidBrush(_nodeColor))
                {
                    g.FillEllipse(brush, nodeRect);
                }

                // 绘制时间文本（节点上方）
                string timeText = _events[i].Time.ToString("yyyy-MM-dd");
                var timeSize = g.MeasureString(timeText, _textFont);
                g.DrawString(
                    timeText,
                    _textFont,
                    new SolidBrush(_textColor),
                    nodeX - timeSize.Width / 2,
                    timelineY - _nodeRadius * 2 - timeSize.Height
                );

                // 绘制事件描述（节点下方）
                string descText = _events[i].Description;
                var descSize = g.MeasureString(descText, _textFont);
                g.DrawString(
                    descText,
                    _textFont,
                    new SolidBrush(_textColor),
                    nodeX - descSize.Width / 2,
                    timelineY + _nodeRadius * 2
                );
            }
        }

        // 当控件尺寸变化时自动重绘
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TimelineControl
            // 
            this.Name = "TimelineControl";
            this.Load += new System.EventHandler(this.TimelineControl_Load);
            this.ResumeLayout(false);

        }

        private void TimelineControl_Load(object sender, EventArgs e)
        {

        }
    }

    // 时间轴事件数据类
    public class TimelineEvent
    {
        public DateTime Time { get; set; }
        public string Description { get; set; }
    }
}