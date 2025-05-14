using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulRuler
{
    // 定义工序层级结构
    public class ProcessNode
    {
        public string Name { get; set; }
        public Color BgColor { get; set; } 
        public List<ProcessNode> Children { get; set; } = new List<ProcessNode>();
    }
}
