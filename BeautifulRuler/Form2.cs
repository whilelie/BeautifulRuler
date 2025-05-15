using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BeautifulRuler.LineViewPanel;

namespace BeautifulRuler
{
    public partial class Form2 : Form
    {
        private LineViewPanel lineViewPanel;
        private DatabaseHelper _dbHelper;

        private TimeAxis timeAxis;
        private DateTime lastVisibleStartTime = DateTime.Today;
        private List<ProcessSegment> allSegments = new List<ProcessSegment>();
        private bool _isScrolling = false;
        private Timer _scrollEndTimer = new Timer { Interval = 150 };
        // 车间及其下属工序
        private List<ProcessNode> processHierarchy = new List<ProcessNode>
        {
            new ProcessNode
            {
                Name = "转炉线",
                Children = new List<ProcessNode>
                {
                    new ProcessNode { Name = "1#转炉" ,BgColor=Color.Chartreuse },
                    new ProcessNode { Name = "2#转炉" ,BgColor=Color.Chartreuse  },
                    new ProcessNode { Name = "1#LF精炼" ,BgColor=Color.DarkSeaGreen  },
                    new ProcessNode { Name = "2#LF精炼" ,BgColor=Color.DarkSeaGreen },
                    new ProcessNode { Name = "3#LF精炼" ,BgColor=Color.DarkSeaGreen},
                    new ProcessNode { Name = "4#LF精炼" ,BgColor=Color.DarkSeaGreen},
                    new ProcessNode { Name = "1#RH精炼",BgColor=Color.ForestGreen },
                    new ProcessNode { Name = "2#RH精炼",BgColor=Color.ForestGreen },
                    new ProcessNode { Name = "1#连铸机" ,BgColor=Color.LightGreen},
                    new ProcessNode { Name = "2#连铸机",BgColor=Color.LightGreen },
                    new ProcessNode { Name = "3#连铸机" ,BgColor=Color.LightGreen}
                }
            },
            new ProcessNode
            {
                Name = "脱磷炉线",
                Children = new List<ProcessNode>
                {
                    new ProcessNode { Name = "3#转炉",BgColor=Color.Chartreuse  },
                    new ProcessNode { Name = "5#LF精炼",BgColor=Color.DarkSeaGreen },
                    new ProcessNode { Name = "6#LF精炼",BgColor=Color.DarkSeaGreen },
                    new ProcessNode { Name = "1#VD精炼" ,BgColor=Color.LightSkyBlue},
                    new ProcessNode { Name = "2#VD精炼" ,BgColor=Color.LightSkyBlue},
                    //new ProcessNode { Name = "4#连铸机",BgColor=Color.LightGreen },
                    new ProcessNode { Name = "5#连铸机" ,BgColor=Color.LightGreen}
                }
            },
            new ProcessNode
            {
                Name = "二车间",
                Children = new List<ProcessNode>
                {
                    new ProcessNode { Name = "量子电炉",BgColor=Color.Chartreuse  },
                    new ProcessNode { Name = "7#LF精炼" ,BgColor=Color.DarkSeaGreen},
                    new ProcessNode { Name = "8#LF精炼" ,BgColor=Color.DarkSeaGreen},
                    new ProcessNode { Name = "3#RH精炼" ,BgColor=Color.ForestGreen},
                    new ProcessNode { Name = "4#RH精炼" ,BgColor=Color.ForestGreen},
                    new ProcessNode { Name = "6#连铸机" ,BgColor=Color.LightGreen}
                }
            }
        };
        private LineElement currentTimeLineElement;
        public Form2()
        {
            InitializeComponent();
            InitializeTimeAxis();

            string connectionString = ConfigurationManager.ConnectionStrings["RmesDb"].ConnectionString;
            _dbHelper = new DatabaseHelper(connectionString);

            checkedListBoxProcess.CheckOnClick = true;
            checkedListBoxProcess.MultiColumn = false;
            //checkedListBoxProcess.ColumnWidth = 70; // 可根据实际宽度调整
            // 只绑定第一级
            checkedListBoxProcess.Items.Clear();
            foreach (var node in processHierarchy)
            {
                checkedListBoxProcess.Items.Add(node.Name);
            }

            // 默认全选
            for (int i = 0; i < this.checkedListBoxProcess.Items.Count; i++)
            {
                this.checkedListBoxProcess.SetItemChecked(i, true);
            }

            PopulateStoveCodeComboBox();

        }
        private void PopulateStoveCodeComboBox()
        {
            try
            {
                List<string> codes = _dbHelper.GetStoveCodes();

                codes.Insert(0, "");

                cmbCode.DataSource = codes;

                if (cmbCode.Items.Count > 0)
                    cmbCode.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载编码错误: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadDataFromDatabase()
        {
            try
            {
                allSegments = _dbHelper.GetProcessSegments(cmbCode.Text);

                if (allSegments.Count > 0)
                {
                    DrawProcessLines();
                }
                else
                {
                    MessageBox.Show("无数据", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据错误: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeTimeAxis()
        {
            // 时间轴控件
            timeAxis = new TimeAxis
            {
                Dock = DockStyle.Top,
                Height = 50,
                PixelsPerHour = 360,
                BackColor = Color.AliceBlue
            };

            // 控制按钮
            var btnPrev = new Button { Text = "向前", Dock = DockStyle.Left };
            var btnNext = new Button { Text = "向后", Dock = DockStyle.Right };

            btnPrev.Click += (s, e) => MoveTimeAxisBackward(); // 每次回退1小时
            btnNext.Click += (s, e) => MoveTimeAxisForward();  // 每次前进1小时


            this.panel2.Controls.Add(timeAxis);
            this.panel1.Controls.Add(btnPrev);
            this.panel1.Controls.Add(btnNext);

        }
        
        // 向前移动时间轴（检查是否已经在当天的00:00）
        private void MoveTimeAxisBackward()
        {
            timeAxis.MoveBackward(1);
            UpdatePanelControlPositions();
        }

        // 向后移动时间轴
        private void MoveTimeAxisForward()
        {
            timeAxis.MoveForward(1);
            UpdatePanelControlPositions();
        }

        // 更新panel5中所有控件的位置
        private void UpdatePanelControlPositions()
        {
            // 获取当前的可见起始时间
            DateTime currentVisibleStartTime = timeAxis.VisibleStartTime;

            // 计算时间差（小时）
            double hourDiff = (lastVisibleStartTime - currentVisibleStartTime).TotalHours;

            // 计算应该移动的像素数（向后为正，向前为负）
            int pixelDiff = (int)(hourDiff * timeAxis.PixelsPerHour);

            // 如果有当前时间线，更新它的位置
            if (currentTimeLineElement != null)
            {
                int x = Convert.ToInt32(timeAxis.GetPosition(DateTime.Now));
                currentTimeLineElement.PointA = new Point(x, 0);
                currentTimeLineElement.PointB = new Point(x, panel5.Height);
            }

            if (pixelDiff != 0)
            {
                // 更新LineViewPanel中所有线条的位置
                lineViewPanel.HorizontalShift(pixelDiff);
            }
            lastVisibleStartTime = currentVisibleStartTime;
            panel5.Invalidate();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            this.Shown += Form2_Shown_SetupLines;
            this.panel5.Paint += Panel5_Paint;

            // 启用双缓冲
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, panel5, new object[] { true });

            SetupScrollHandlers();

            // 初始化LineViewPanel并添加到panel5
            lineViewPanel = new LineViewPanel
            {
                // 不使用Dock=Fill，而是设置Location和Size
                Location = new Point(120, 0),  // 从工序标签右侧开始
                Size = new Size(panel5.Width - 120, panel5.Height),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            panel5.Controls.Add(lineViewPanel);
            lineViewPanel.BringToFront();  // 确保在最上层
        }

        private void Form2_Shown_SetupLines(object sender, EventArgs e)
        {
            this.Shown -= Form2_Shown_SetupLines;
       
            GenerateProcessLabels();  // 先生成标签
            LoadDataFromDatabase();   // 加载数据并绘制

            // 设置当前时间线
            int x = Convert.ToInt32(timeAxis.GetPosition(DateTime.Now));
            currentTimeLineElement = new LineElement
            {
                PointA = new Point(x, 0),
                PointB = new Point(x, panel5.Height),
                LineColor = Color.Blue,
                LineWidth = 2f
            };

            panel5.Invalidate();
            //BtnRefresh_Click(sender, e); // Refresh the panel to update positions
        }
        private void Panel5_Paint(object sender, PaintEventArgs e)
        {
            // 仅在非快速滚动时绘制网格线
            if (!_isScrolling)
            {
                PaintPanelGrid(e);
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            if (currentTimeLineElement != null)
            {
                using (Pen pen = new Pen(currentTimeLineElement.LineColor, currentTimeLineElement.LineWidth))
                {
                    e.Graphics.DrawLine(pen, currentTimeLineElement.PointA, currentTimeLineElement.PointB);
                }
            }
        }
        private void SetupScrollHandlers()
        {
            _scrollEndTimer.Tick += (s, e) => {
                _scrollEndTimer.Stop();
                _isScrolling = false;
                panel5.Invalidate(); // 滚动停止后重绘一次
            };

            // 处理滚动事件
            panel5.Scroll += (s, e) => {
                if (!_isScrolling)
                {
                    _isScrolling = true;
                }
                _scrollEndTimer.Stop();
                _scrollEndTimer.Start(); // 重置计时器
            };
        }
        private void PaintPanelGrid(PaintEventArgs e)
        {
            var x1 = 0;
            var x2 = this.panel5.Width;

            // 获取面板的可见区域
            Rectangle visibleRect = panel5.ClientRectangle;

            // 获取当前panel5中所有非一级工序标题的Label（只保留二级工序的Label）
            var processLabels = panel5.Controls.OfType<Label>()
                .Where(lbl => !lbl.AutoSize) // 二级工序Label不是AutoSize的，一级工序的标题是AutoSize的
                .OrderBy(lbl => lbl.Top)
                .ToList();

            // 绘制横线
            using (var pen = new Pen(Color.Black, 1))
            {
                // 绘制顶部线（仅当顶部可见时）
                if (visibleRect.Top <= 0)
                {
                    e.Graphics.DrawLine(pen, x1, 0, x2, 0);
                }

                // 仅在每个可见的二级工序的底部绘制横线
                foreach (var label in processLabels)
                {
                    int y = label.Top + label.Height;

                    // 只绘制可见区域内的线条
                    if (y >= visibleRect.Top && y <= visibleRect.Bottom)
                    {
                        e.Graphics.DrawLine(pen, x1, y, x2, y);
                    }
                }
            }
        }
        

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            timeAxis.CenterCurrentTime();
            UpdatePanelControlPositions();
        }


        private void BtnSearch_Click(object sender, EventArgs e)
        {
            GenerateProcessLabels();
            LoadDataFromDatabase();
        }

        private void GenerateProcessLabels()
        {
            // 清除panel5中所有Label控件
            panel5.Controls.OfType<Label>().ToList().ForEach(lbl => panel5.Controls.Remove(lbl));

            // 获取选中的工序
            var checkedItems = checkedListBoxProcess.CheckedItems;
            int labelHeight = 60;
            int labelWidth = 121;
            int fontSize = 14;

            int labelIndex = 0;

            foreach (var checkedItem in checkedItems)
            {
                // 找到对应的 ProcessNode
                var node = processHierarchy.FirstOrDefault(n => n.Name == checkedItem.ToString());
                if (node != null && node.Children != null && node.Children.Count > 0)
                {
                    bool isFirst = true; // 标记是否是该一级工序下的第一个二级工序

                    // 添加该车间下的所有二级工序
                    foreach (var child in node.Children)
                    {
                        var label = new Label
                        {
                            Text = child.Name,
                            Size = new Size(labelWidth, labelHeight),
                            Location = new Point(0, labelIndex * labelHeight),
                            Font = new Font("宋体", fontSize, FontStyle.Regular),
                            TextAlign = ContentAlignment.MiddleCenter,
                            BackColor = child.BgColor
                        };
                        panel5.Controls.Add(label);

                        // 如果是该一级工序下的第一个二级工序，添加一级工序名称标签
                        if (isFirst)
                        {
                            var headerLabel = new Label
                            {
                                Text = node.Name,
                                AutoSize = true,
                                Location = new Point(2, labelIndex * labelHeight + 2), // 左上角位置，留出小边距
                                Font = new Font("宋体", fontSize, FontStyle.Bold),
                                BackColor = child.BgColor
                            };
                            panel5.Controls.Add(headerLabel);
                            headerLabel.BringToFront(); // 确保显示在最上层
                            isFirst = false;
                        }

                        labelIndex++;
                    }
                }
            }
            // 重新绘制panel5
            panel5.Invalidate();
        }
        private void DrawProcessLines()
        {
            // 清除所有线条
            lineViewPanel.ClearLines();

            // 获取选中的第一级工序
            var selectedTopLevel = checkedListBoxProcess.CheckedItems.Cast<string>().ToList();

            // 获取所有选中一级工序下的所有二级工序名
            var selectedSecondLevel = processHierarchy
                .Where(node => selectedTopLevel.Contains(node.Name))
                .SelectMany(node => node.Children ?? new List<ProcessNode>())
                .Select(child => child.Name)
                .ToList();

            // 按 Ty 分组
            var grouped = allSegments
                .Where(seg => selectedSecondLevel.Contains(seg.ProcessName))
                .GroupBy(seg => seg.Ty)
                .ToList();

            // 创建两个列表，分别存储水平线和斜线
            var horizontalLines = new List<LineInfo>();
            var diagonalLines = new List<LineInfo>();

            // 首先收集所有需要绘制的线条信息
            foreach (var group in grouped)
            {
                var segs = group.OrderBy(s => s.StartTime).ToList();
                if (segs.Count == 0) continue;

                // 收集所有线条信息
                for (int i = 0; i < segs.Count; i++)
                {
                    var seg = segs[i];
                    int y = GetProcessY(seg.ProcessName);
                    float x1 = timeAxis.GetPosition(seg.StartTime);
                    float x2 = timeAxis.GetPosition(seg.EndTime);

                    // 添加水平工序线
                    lineViewPanel.AddLine(
                        new Point((int)x1, y),
                        new Point((int)x2, y),
                        seg.Ty,
                        seg.SteelNo);

                    // 画连接线（如果有下一个段）
                    if (i < segs.Count - 1)
                    {
                        var next = segs[i + 1];
                        int nextY = GetProcessY(next.ProcessName);
                        float x3 = timeAxis.GetPosition(next.StartTime);

                        // 添加斜线（连接线）
                        lineViewPanel.AddLine(
                            new Point((int)x2, y),
                            new Point((int)x3, nextY),
                            "",
                            "");
                    }
                }
            }
        }

        private int GetProcessY(string processName)
        {
            // 找到panel5中对应工序label的Y坐标
            var labels = panel5.Controls.OfType<Label>().ToList();
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i].Text == processName)
                    return labels[i].Top + labels[i].Height / 2;
            }
            return 0;
        }

        private void Form2_Resize(object sender, EventArgs e)
        {
            if (lineViewPanel != null)
            {
                lineViewPanel.Width = panel5.Width - 120;
                lineViewPanel.Height = panel5.Height;
            }
        }
    }


    public class ProcessSegment
    {
        public string ProcessName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Ty { get; set; }
        public string SteelNo { get; set; }
    }

}
