using System.Drawing;

public class LineElement
{
    public Point PointA { get; set; }
    public Point PointB { get; set; }
    public Color LineColor { get; set; }
    public float LineWidth { get; set; }
    public string LabelText { get; set; }
    public Point LabelLocation { get; set; } 
    public bool IsVerticalTimeMarker { get; set; } = false;
}