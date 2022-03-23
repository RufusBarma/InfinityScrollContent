using Reddit;
using Reddit.Controllers;

var appId = GetVariable("app_id_reddit");
var refreshToken = GetVariable("refresh_token_reddit");
var appSecret = GetVariable("app_secret_reddit");
var accessToken = GetVariable("access_token_reddit");

var reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);

var subreddit = reddit.Subreddit("all", over18: true);
List<Post> posts = subreddit.Search();

string GetVariable(string variable)
{
	var envVariable = Environment.GetEnvironmentVariable(variable);
	if (envVariable == null)
	{
		throw new Exception($"Can't get '{variable}' from environment!");
	}

	return envVariable;
}