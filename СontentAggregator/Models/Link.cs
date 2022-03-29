namespace СontentAggregator.Models;

public enum LinkType
{
    Video,
    Img,
    Gif,
    Album
}
public record Link
{
    public string Value { get; init; }
    public Category Category { get; init; }
    public LinkType Type { get; init; }
}