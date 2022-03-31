namespace СontentAggregator.Models;

public enum LinkType
{
    None,
    Video,
    Img,
    Gif,
    Album
}
public record Link
{
    public string SourceUrl { get; init; }
    public string[] Urls { get; init; }
    public string Domain { get; init; }
    public string Category { get; init; }
    public LinkType Type { get; init; }
}