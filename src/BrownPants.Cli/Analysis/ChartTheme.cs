namespace BrownPants.Cli.Analysis;

internal readonly record struct ChartColor(byte Red, byte Green, byte Blue)
{
    internal string ToHex() => $"#{Red:x2}{Green:x2}{Blue:x2}";
    internal byte[] ToRgb() => [Red, Green, Blue];

    internal double ContrastRatio(ChartColor other)
    {
        var first = RelativeLuminance();
        var second = other.RelativeLuminance();
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private double RelativeLuminance() =>
        0.2126 * Linearize(Red / 255d) + 0.7152 * Linearize(Green / 255d) + 0.0722 * Linearize(Blue / 255d);

    private static double Linearize(double channel) =>
        channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}

internal sealed record ChartShapeColors(ChartColor Fill, ChartColor Stroke);

internal sealed record ChartTheme(
    string Name,
    int SchemaVersion,
    ChartColor Canvas,
    ChartColor Text,
    ChartColor Connector,
    ChartColor EdgeLabelBackground,
    ChartColor EdgeLabelText,
    ChartColor Shadow,
    IReadOnlyDictionary<LocalFlowchartNodeShape, ChartShapeColors> Shapes)
{
    internal bool HasAccessibleTextContrast =>
        Shapes.Values.All(shape => Text.ContrastRatio(shape.Fill) >= 4.5)
        && EdgeLabelText.ContrastRatio(EdgeLabelBackground) >= 4.5;

    internal ChartShapeColors Shape(LocalFlowchartNodeShape shape) => Shapes[shape];
}

internal static class ChartThemeCatalog
{
    private static readonly IReadOnlyDictionary<string, ChartTheme> Themes =
        new Dictionary<string, ChartTheme>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = Create("default", "#f7f9fc", "#223146", "#7c8da4", "#f7f9fc", "#223146", "#e0e7f0",
                "#e0f5eb", "#e8f1fa", "#fff5d6", "#e4f3fd", "#e8f1ff", "#fff7e1", "#e0f5eb", "#243447"),
            ["blue-water"] = Create("blue-water", "#eaf6fa", "#16323d", "#28758c", "#ffffff", "#16323d", "#c9e1e7",
                "#cdefe3", "#ffffff", "#cde8f5", "#c1e4ea", "#dceef3", "#e3f2f2", "#cdefe3", "#1f6475"),
            ["high-contrast"] = Create("high-contrast", "#ffffff", "#000000", "#111111", "#ffffff", "#000000", "#dddddd",
                "#d9ffd9", "#ffffff", "#ffff00", "#e6e6e6", "#cceeff", "#ffddff", "#d9ffd9", "#000000")
        };

    internal static ChartTheme Default => Themes["default"];
    internal static IEnumerable<ChartTheme> All => Themes.Values.OrderBy(theme => theme.Name, StringComparer.Ordinal);

    internal static bool TryGet(string name, out ChartTheme theme) => Themes.TryGetValue(name, out theme!);

    private static ChartTheme Create(string name, string canvas, string text, string connector, string edgeLabelBackground,
        string edgeLabelText, string shadow, string start, string process, string decision, string inputOutput,
        string person, string cloud, string end, string stroke)
    {
        var sharedStroke = Parse(stroke);
        return new ChartTheme(name, 1, Parse(canvas), Parse(text), Parse(connector), Parse(edgeLabelBackground),
            Parse(edgeLabelText), Parse(shadow), new Dictionary<LocalFlowchartNodeShape, ChartShapeColors>
            {
                [LocalFlowchartNodeShape.Start] = new(Parse(start), sharedStroke),
                [LocalFlowchartNodeShape.Process] = new(Parse(process), sharedStroke),
                [LocalFlowchartNodeShape.Decision] = new(Parse(decision), sharedStroke),
                [LocalFlowchartNodeShape.InputOutput] = new(Parse(inputOutput), sharedStroke),
                [LocalFlowchartNodeShape.Person] = new(Parse(person), sharedStroke),
                [LocalFlowchartNodeShape.Cloud] = new(Parse(cloud), sharedStroke),
                [LocalFlowchartNodeShape.End] = new(Parse(end), sharedStroke)
            });
    }

    private static ChartColor Parse(string hex) => new(
        Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));
}
