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
        //private Button btnPrev;
        //private Button btnNext;
        //private TimeAxis timeAxis = new TimeAxis();
        //private Button btnRefresh;
        private static DateTime lastVisibleStartTime = DateTime.Today;
        private List<LineElement> linesToDraw = new List<LineElement>();
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

            //allSegments = new List<ProcessSegment>
            //{
            //    new ProcessSegment { ProcessName = "1#转炉", StartTime = new DateTime(2025,5,14,6,30,0), EndTime = new DateTime(2025,5,14,7,0,0), Ty = "61905035" },
            //    new ProcessSegment { ProcessName = "2#转炉", StartTime = new DateTime(2025,5,14,7,10,0), EndTime = new DateTime(2025,5,14,7,40,0), Ty = "61905035" },
            //    new ProcessSegment { ProcessName = "3#转炉", StartTime = new DateTime(2025,5,14,7,50,0), EndTime = new DateTime(2025,5,14,8,20,0), Ty = "61905035" },
            //    //new ProcessSegment { ProcessName = "4#转炉", StartTime = new DateTime(2025,5,14,8,30,0), EndTime = new DateTime(2025,5,14,9,30,0), Ty = "61905035" },

            //    //new ProcessSegment { ProcessName = "1#转炉", StartTime = new DateTime(2025,5,14,4,30,0), EndTime = new DateTime(2025,5,14,5,0,0), Ty = "61905036" },
            //    //new ProcessSegment { ProcessName = "4#转炉", StartTime = new DateTime(2025,5,14,6,30,0), EndTime = new DateTime(2025,5,14,7,30,0), Ty = "61905036" },
            //    // ... 其他工序段
            //};
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                // Get process segments from database
                allSegments = _dbHelper.GetProcessSegments(txtCode.Text);

                // If we got any data, update the UI
                if (allSegments.Count > 0)
                {
                    DrawProcessLines();
                }
                else
                {
                    MessageBox.Show("No process data found in the database.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error",
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

            //var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            this.panel2.Controls.Add(timeAxis);
            this.panel1.Controls.Add(btnPrev);
            this.panel1.Controls.Add(btnNext);
            //Controls.Add(timeAxis);
            //Controls.Add(panel);
        }
        private void InitializeUI()
        {
            // 时间轴控件
            timeAxis = new TimeAxis
            {
                Dock = DockStyle.Top,
                Height = 150,
                PixelsPerHour = 80,
                //AutoCenterCurrentTime = false // 关闭自动居中
            };

            // 刷新按钮
            //btnRefresh = new Button
            //{
            //    Text = "居中当前时间",
            //    Dock = DockStyle.Bottom,
            //    Height = 40
            //};
            //btnRefresh.Click += BtnRefresh_Click;

            // 控制按钮容器
            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = SystemColors.Control
            };

            // 添加其他控制按钮（示例）
            var btnZoomIn = new Button { Text = "放大", Dock = DockStyle.Left, Width = 80 };
            var btnZoomOut = new Button { Text = "缩小", Dock = DockStyle.Left, Width = 80 };

            btnZoomIn.Click += (s, e) => timeAxis.PixelsPerHour *= 1.2f;
            btnZoomOut.Click += (s, e) => timeAxis.PixelsPerHour /= 1.2f;

            //controlPanel.Controls.AddRange(new Control[] { btnZoomIn, btnZoomOut, btnRefresh });

            // 添加控件到窗体
            Controls.Add(timeAxis);
            Controls.Add(controlPanel);
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
            //WriteLine(new Point(130, 30), new Point(230, 30), "11111");
            //WriteLine(new Point(130, 100), new Point(230, 100), "22222");
            this.Shown += Form2_Shown_SetupLines;
            this.panel5.Paint += Panel5_Paint;
            GenerateProcessLabels();
         
        }

        private void Form2_Shown_SetupLines(object sender, EventArgs e)
        {
            try
            {
                this.Shown -= Form2_Shown_SetupLines;
                // 清空panel5上的所有LineControl
                var controlsToRemove = panel5.Controls.OfType<LineControl>().ToList();
                foreach (var ctrl in controlsToRemove)
                    panel5.Controls.Remove(ctrl);
                DrawProcessLines();
                // 添加线和标签
                //WriteLine(new Point(125, 20), new Point(230, 20), "619050325");
                //WriteLine(new Point(230, 20), new Point(250, 60), "");
                //WriteLine(new Point(250, 60), new Point(350, 60), "619050325");
                //WriteLine(new Point(350, 60), new Point(400, 140), "");
                //WriteLine(new Point(400, 140), new Point(480, 140), "619050325");


                // Setup current time line
                int x = Convert.ToInt32(timeAxis.GetPosition(DateTime.Now));
                currentTimeLineElement = new LineElement
                {
                    PointA = new Point(x, 0),
                    PointB = new Point(x, panel5.Height),
                    LineColor = Color.Blue,
                    LineWidth = 2f,
                    IsVerticalTimeMarker = true
                };

                panel5.Invalidate(); // Trigger Panel5_Paint
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPTION in Form2_Shown_SetupLines: {ex.ToString()}");
                MessageBox.Show($"Error in Form2_Shown_SetupLines: {ex.Message}\n\n{ex.StackTrace}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //BtnRefresh_Click(sender, e); // Refresh the panel to update positions
        }
        private void Panel5_Paint(object sender, PaintEventArgs e)
        {
            //画网格线
            PaintPanelGrid(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            //foreach (var lineElement in linesToDraw)
            //{
            //    using (Pen pen = new Pen(lineElement.LineColor, lineElement.LineWidth))
            //    {
            //        e.Graphics.DrawLine(pen, lineElement.PointA, lineElement.PointB);
            //    }
            //    if (!string.IsNullOrEmpty(lineElement.LabelText))
            //    {
            //        TextRenderer.DrawText(e.Graphics, lineElement.LabelText, this.Font, lineElement.LabelLocation, Color.Black, TextFormatFlags.Default);
            //    }
            //}

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

            // 获取当前panel5中所有Label
            var labels = panel5.Controls.OfType<Label>().ToList();
            int labelCount = labels.Count;
            int labelHeight = labelCount > 0 ? labels[0].Height : 40; // 默认40

            // 绘制横线
            using (var pen = new Pen(Color.Black, 1))
            {
                for (var i = 0; i <= labelCount; i++)
                {
                    var y = labelHeight * i;
                    e.Graphics.DrawLine(pen, x1, y, x2, y);
                }
            }
        }
        public void WriteLine(Point start, Point end, string ty,string no)
        {
            //if (ty.Length > 1 && ty != null)
            //{
            //    Label label = new Label();
            //    label.Text = ty;
            //    label.Location = new Point(start.X, start.Y - 30); // 设置标签在窗体中的位置
            //    label.TabIndex = 0;
            //    label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;//居中显示
            //    label.BackColor = Color.Transparent;  // 关键属性
            //    label.UseCompatibleTextRendering = true;  // 启用GDI+渲染
            //    this.panel5.Controls.Add(label); // 将标签添加到窗体中

            //}
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
            Console.WriteLine($"Add LineControl: {start} -> {end}, ty={ty}");

            //Label label = new Label();
            //label.Text = "Hello, World!";
            //label.Location = new Point(start.X, start.Y - 30); // 设置标签在窗体中的位置
            //label.TabIndex = 0;
            //label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;//居中显示
            //label.BackColor = Color.Transparent;  // 关键属性
            //label.UseCompatibleTextRendering = true;  // 启用GDI+渲染
            //this.Controls.Add(label); // 将标签添加到窗体中

            //var lineControl = new LineControl
            //{
            //    Dock = DockStyle.Fill,
            //    _pointA = start,
            //    _pointB = end
            //};
            //Controls.Add(lineControl);
        }

        private void button1_Click(object sender, EventArgs e)
        {



            //Point screenCoordinates = button1.PointToScreen(Point.Empty);
            //Point screenPoint = Control.MousePosition;


            Point screenPoint = label1.PointToScreen(Point.Empty);

            //label0000.Text = $"按钮屏幕坐标：X={screenPoint.X}, Y={screenPoint.Y}";
            //Point screenPoint1 = label11.PointToScreen(Point.Empty);
            //label0001.Text = $"按钮屏幕坐标：X={screenPoint1.X}, Y={screenPoint1.Y}";

            //WriteLine(new Point(100, 200), new Point(200, 300), "测试炉号");

        }



        private void Form2_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            timeAxis.CenterCurrentTime();
            //int x =Convert.ToInt32(  timeAxis.GetPosition(DateTime.Now));

            //WriteLine(new Point(x, 0), new Point(x, 3000), "测试炉号");
            UpdatePanelControlPositions();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {

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
                if (node != null && node.Children != null)
                {
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
                    WriteLine(new Point((int)x1, y), new Point((int)x2, y), seg.Ty,seg.SteelNo);

                    // 画连接线（如果有下一个段）
                    if (i < segs.Count - 1)
                    {
                        var next = segs[i + 1];
                        int nextY = GetProcessY(next.ProcessName);
                        float x3 = timeAxis.GetPosition(next.StartTime);

                        // 连接线：起点为当前段的结束点，终点为下一个段的起点
                        WriteLine(new Point((int)x2, y), new Point((int)x3, nextY), "","");
                    }
                }
            }
        }



        // 可选：自定义不同label的背景色
        private Color GetLabelColor(int index)
        {
            int count = checkedListBoxProcess.Items.Count;
            // 预设一组基础颜色
            Color[] baseColors = new Color[]
            {
        Color.Chartreuse, Color.DarkSeaGreen, Color.ForestGreen, Color.LightGreen,
        Color.LightSkyBlue, Color.LightSalmon, Color.Khaki, Color.Plum,
        Color.LightPink, Color.LightSteelBlue, Color.PaleTurquoise, Color.Orange
            };

            if (count <= baseColors.Length)
            {
                return baseColors[index % baseColors.Length];
            }
            else
            {
                // 超过基础色数量，自动生成不同色相的颜色
                // HSL色环均分
                double hue = (360.0 * index) / count;
                return FromHsl(hue, 0.5, 0.7);
            }
        }

        // HSL转Color的辅助方法
        private Color FromHsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            int R = (int)((r + m) * 255);
            int G = (int)((g + m) * 255);
            int B = (int)((b + m) * 255);
            return Color.FromArgb(R, G, B);
        }

        int GetProcessY(string processName)
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

        //protected override void OnMouseWheel(MouseEventArgs e)
        //{
        //    base.OnMouseWheel(e);
        //    if (e.Delta > 0) MoveBackward(1);
        //    else MoveForward(1);
        //}

        //private void DrawCurrentTimeMarker(Graphics g)
        //{
        //    DateTime now = DateTime.Now;
        //    if (now >= VisibleStartTime && now <= VisibleEndTime)
        //    {
        //        float x = GetPosition(now);
        //        using (Pen markerPen = new Pen(Color.Red, 2))
        //        {
        //            g.DrawLine(markerPen, x, 0, x, Height);
        //        }
        //    }
        //}

        //public DateTime GetTimeAtPosition(Point point)
        //{
        //    return VisibleStartTime.AddHours(point.X / PixelsPerHour);
        //}
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
