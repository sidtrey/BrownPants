namespace BrownPants.Cli.Analysis;

internal sealed record LocalFlowchartTileManifest(int SchemaVersion, string DiagramId, string SourcePath,
    int CanvasWidth, int CanvasHeight, int TileSize, int RequestedOverlap,
    IReadOnlyList<LocalFlowchartTileManifestEntry> Tiles);

internal sealed record LocalFlowchartTileManifestEntry(long Order, int Row, int Column, string Image,
    string EditableSource, int X, int Y, int Width, int Height,
    int OverlapWithLeft, int OverlapWithTop);
