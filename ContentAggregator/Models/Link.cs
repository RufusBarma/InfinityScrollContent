namespace ContentAggregator.Models;

public abstract record Link
{
	public string PermaLink { get; init; }
    public string SourceUrl { get; init; }
    public string[] Category { get; init; }
    public string Description { get; init; }
}

public record RedditLink: Link
{
    public string FullName { get; init; }
    public string Subreddit { get; init; }
    public int UpVotes { get; init; }
    public double UpvoteRatio { get; init; }
}