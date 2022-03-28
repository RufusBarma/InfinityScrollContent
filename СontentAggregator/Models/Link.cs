namespace СontentAggregator.Models;

public enum LinkType
{
    Video,
    Img,
    Album
}
public record Links
{
    public List<string> Value { get; init; }
    public Category Category { get; init; }
    public LinkType Type { get; init; }
}