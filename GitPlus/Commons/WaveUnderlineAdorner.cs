using System.Windows.Documents;

namespace GitPlus.Commons;

public sealed class WaveUnderlineAdorner : Adorner
{
    private readonly int start;
    private readonly int length;
    private readonly Brush brush;

    public WaveUnderlineAdorner(TextBox textBox, TextSpan span, Brush brush) : this(textBox, span.Start, span.Length, brush) { }

    public WaveUnderlineAdorner(TextBox textBox, int start, int length, Brush brush) : base(textBox)
    {
        this.start = start;
        this.length = length;
        this.brush = brush;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        try
        {
            var textBox = (TextBox)AdornedElement;
            if (textBox.Text.Length == 0) { return; }
            int end = Math.Min(start + length, textBox.Text.Length);
            if (end <= 0 || end < start) { return; }

            double charWidth = GetCharWidth(textBox, "a");

            var lines = GetRenderLines(textBox, start, end);

            var geometry = new StreamGeometry();
            using var context = geometry.Open();
            foreach (var line in lines)
            {
                const double halfWave = 3.0;
                const double amplitude = 1.8;
                var right = line.Right == line.X ? line.Right + charWidth : line.Right;
                CreateWaveGeometry(context, line.X, right, line.Bottom, halfWave, amplitude);
            }
            if (geometry.CanFreeze)
            { geometry.Freeze(); }
            dc.DrawGeometry(null, new Pen(brush, 1.0), geometry);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rendering wave underline adorner.");
        }
    }

    private static double GetCharWidth(TextBox textBox, string textToFormat = "a")
    {
        var typeface = new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch);
        var culture = textBox.Language.GetSpecificCulture();
        var pixelsPerDip = VisualTreeHelper.GetDpi(textBox).PixelsPerDip;
        var formattedText = new FormattedText(textToFormat, culture, textBox.FlowDirection, typeface, textBox.FontSize, textBox.Foreground, pixelsPerDip);
        return formattedText.Width;
    }

    private static List<Rect> GetRenderLines(TextBox textBox, int start, int end)
    {
        var lines = new List<Rect>();
        var charRect = textBox.GetRectFromCharacterIndex(start);
        if (charRect.IsEmpty) { return lines; }
        int lineIndex = textBox.GetLineIndexFromCharacterIndex(start);
        if (lineIndex < 0) { return lines; }
        int startLineIndex = lineIndex;
        var lineStartPoint = new Point(charRect.X, charRect.Bottom);
        var lineEndPoint = new Point(charRect.X, charRect.Bottom);
        for (int index = start + 1; index <= end; index++)
        {
            var currentCharRect = textBox.GetRectFromCharacterIndex(index);
            if (currentCharRect.IsEmpty) { return lines; }
            var currentLineIndex = textBox.GetLineIndexFromCharacterIndex(index);
            if (currentLineIndex < 0) { return lines; }
            if (currentLineIndex != lineIndex)
            {
                lineEndPoint = new Point(charRect.X, charRect.Bottom);
                lines.Add(new Rect(lineStartPoint, lineEndPoint));
                lineStartPoint = new Point(currentCharRect.X, currentCharRect.Bottom);
                lineIndex = currentLineIndex;
                startLineIndex = lineIndex;
            }
            lineEndPoint = new Point(currentCharRect.X, currentCharRect.Bottom);
            charRect = currentCharRect;
        }
        if (lineIndex == startLineIndex)
        {
            lines.Add(new Rect(lineStartPoint, lineEndPoint));
        }

        return lines;
    }

    private static void CreateWaveGeometry(StreamGeometryContext ctx, double start, double end, double baseline, double halfWave, double amplitude)
    {
        ctx.BeginFigure(new Point(start, baseline), false, false);

        double x = start;
        bool up = true;
        while (x < end)
        {
            double next = Math.Min(x + halfWave, end);

            double length = next - x;
            double direction = up ? -1 : 1;

            var p0 = new Point(x, baseline);
            var p3 = new Point(next, baseline);
            var p1 = new Point(x + length / 3, baseline + direction * amplitude);
            var p2 = new Point(next - length / 3, baseline + direction * amplitude);
            ctx.BezierTo(p1, p2, p3, true, false);
            x = next;
            up = !up;
        }
    }

}