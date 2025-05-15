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

namespace BeautifulRuler
{
    public partial class Form2 : Form
    {

        private DatabaseHelper _dbHelper;

        private TimeAxis timeAxis;
        private static DateTime lastVisibleStartTime = DateTime.Today;
        private List<ProcessSegment> allSegments = new List<ProcessSegment>();

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
            //InitializeUI();

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

            LoadDataFromDatabase();
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
                MessageBox.Show($"加载编码错误: {ex.Message}", "Error",
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
                    MessageBox.Show("无数据", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据错误: {ex.Message}", "Database Error",
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
                // 更新panel5中所有LineControl的位置
                foreach (Control ctrl in panel5.Controls)
                {
                    if (ctrl is LineControl lineCtrl)
                    {
                        lineCtrl.Left += pixelDiff;
                    }
                }
            }
            lastVisibleStartTime = currentVisibleStartTime;
            panel5.Invalidate();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            this.Shown += Form2_Shown_SetupLines;
            this.panel5.Paint += Panel5_Paint;
            GenerateProcessLabels();

        }

        private void Form2_Shown_SetupLines(object sender, EventArgs e)
        {
            this.Shown -= Form2_Shown_SetupLines;
            // 清空panel5上的所有LineControl
            var controlsToRemove = panel5.Controls.OfType<LineControl>().ToList();
            foreach (var ctrl in controlsToRemove)
                panel5.Controls.Remove(ctrl);

            DrawProcessLines();

            // 设置当前时间线
            int x = Convert.ToInt32(timeAxis.GetPosition(DateTime.Now));
            currentTimeLineElement = new LineElement
            {
                PointA = new Point(x, 0),
                PointB = new Point(x, panel5.Height),
                LineColor = Color.Blue,
                LineWidth = 2f,
                IsVerticalTimeMarker = true
            };

            panel5.Invalidate();
            //BtnRefresh_Click(sender, e); // Refresh the panel to update positions
        }
        private void Panel5_Paint(object sender, PaintEventArgs e)
        {
            //画网格线
            PaintPanelGrid(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            if (currentTimeLineElement != null)
            {
                using (Pen pen = new Pen(currentTimeLineElement.LineColor, currentTimeLineElement.LineWidth))
                {
                    e.Graphics.DrawLine(pen, currentTimeLineElement.PointA, currentTimeLineElement.PointB);
                }
            }
        }

        private void PaintPanelGrid(PaintEventArgs e)
        {
            var x1 = 0;
            var x2 = this.panel5.Width;

            // 获取当前panel5中所有非一级工序标题的Label（只保留二级工序的Label）
            var processLabels = panel5.Controls.OfType<Label>()
                .Where(lbl => !lbl.AutoSize) // 二级工序Label不是AutoSize的，一级工序的标题是AutoSize的
                .OrderBy(lbl => lbl.Top)
                .ToList();

            // 绘制横线
            using (var pen = new Pen(Color.Black, 1))
            {
                // 绘制顶部线
                e.Graphics.DrawLine(pen, x1, 0, x2, 0);

                // 仅在每个二级工序的底部绘制横线
                foreach (var label in processLabels)
                {
                    int y = label.Top + label.Height;
                    e.Graphics.DrawLine(pen, x1, y, x2, y);
                }
            }
        }
        public void WriteLine(Point start, Point end, string ty, string no)
        {
            // 计算线段的起点和终点相对位置，以确定控件的大小和位置
            int minX = Math.Min(start.X, end.X);
            int minY = Math.Min(start.Y, end.Y);
            int width = Math.Abs(end.X - start.X) + 5; // 增加一些边距
            int height = Math.Abs(end.Y - start.Y) + 50; // 增加上方空间给label

            width = Math.Max(width, 1);
            height = Math.Max(height, 1);

            var lineControl = new LineControl
            {
                Size = new Size(width, height),
                Location = new Point(minX - 5, minY - 25), // 上移以给label空间
                _pointA = new Point(start.X - (minX - 5), start.Y - (minY - 25)),
                _pointB = new Point(end.X - (minX - 5), end.Y - (minY - 25)),
                LabelText = ty,
                No = no,
                LabelOffset = new Point(0, -10) // 可根据需要调整
            };
            lineControl.EnableTransparentBackground();
            this.panel5.Controls.Add(lineControl);
            //panel5.Controls.SetChildIndex(lineControl, 0); // 保证线在label下方
            if (!string.IsNullOrEmpty(ty) || !string.IsNullOrEmpty(no))
            {
                //lineControl.BringToFront();
            }
            Console.WriteLine($"Add LineControl: {start} -> {end}, ty={ty}");
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            timeAxis.CenterCurrentTime();
            //int x =Convert.ToInt32(  timeAxis.GetPosition(DateTime.Now));

            //WriteLine(new Point(x, 0), new Point(x, 3000), "测试炉号");
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
            // 清除旧的LineControl
            var oldLines = panel5.Controls.OfType<LineControl>().ToList();
            foreach (var line in oldLines)
                panel5.Controls.Remove(line);

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

            foreach (var group in grouped)
            {
                var segs = group.OrderBy(s => s.StartTime).ToList();
                if (segs.Count == 0) continue;

                // 顺序：先画第一条线，再画连接线，再画第二条线……
                for (int i = 0; i < segs.Count; i++)
                {
                    var seg = segs[i];
                    int y = GetProcessY(seg.ProcessName);
                    float x1 = timeAxis.GetPosition(seg.StartTime);
                    float x2 = timeAxis.GetPosition(seg.EndTime);
                    WriteLine(new Point((int)x1, y), new Point((int)x2, y), seg.Ty, seg.SteelNo);

                    // 画连接线（如果有下一个段）
                    if (i < segs.Count - 1)
                    {
                        var next = segs[i + 1];
                        int nextY = GetProcessY(next.ProcessName);
                        float x3 = timeAxis.GetPosition(next.StartTime);

                        // 连接线：起点为当前段的结束点，终点为下一个段的起点
                        WriteLine(new Point((int)x2, y), new Point((int)x3, nextY), "", "");
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
