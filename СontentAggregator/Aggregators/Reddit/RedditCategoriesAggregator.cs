using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace СontentAggregator.Aggregators.Reddit;

public record Category
{
	public string Title { get; init; }
	public List<string> MasterCategories { get; init; }
	public string Description { get; init; }
}

public class RedditCategoriesAggregator
{
	private string _cachePath; 
	private string _categoriesAddress; 

	public RedditCategoriesAggregator(IConfiguration config)
	{
		_cachePath = config["Cache:CategoriesPath"];
		_categoriesAddress = config["Reddit:CategoriesAddress"];
	}

	public List<Category> GetCategories()
	{
		if (File.Exists(_cachePath))
			return JsonSerializer.Deserialize<List<Category>>(File.ReadAllText(_cachePath));
		var categories = ParseCategories(_categoriesAddress);
		var categoriesJson = JsonSerializer.Serialize(categories);
		new FileInfo(_cachePath).Directory?.Create();
		File.WriteAllText(_cachePath, categoriesJson);
		return categories;
	}

	private List<Category> ParseCategories(string address)
	{
		var webClient = new WebClient();
		var page = webClient.DownloadString(address);

		var doc = new HtmlAgilityPack.HtmlDocument();
		doc.LoadHtml(page);
		doc.OptionEmptyCollection = true;

		var categories = doc.DocumentNode
			.SelectNodes("//h1")
			.Select(GetSubCategories)
			.SelectMany(highCategory => highCategory.SubCategories
					.SelectMany(category => 
						category.Table.Select(row => new Category
						{
							Title = row.First(), 
							Description = row.Last(),
							MasterCategories = new []{highCategory.Title, category.Title}
								.Where(s => !string.IsNullOrEmpty(s))
								.ToList()
						})))
			.ToList();
		return categories;
	}

	private List<List<string>> ParseTable(HtmlNode tableNode)
	{
		return tableNode
			.Descendants("tr")
			.Skip(1)
			.Where(tr=>tr.Elements("td").Count()>1)
			.Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
			.ToList();
	}

	private (string Title, List<(string Title, List<List<string>> Table)> SubCategories) GetSubCategories(HtmlNode parentNode)
	{
		var subCategories = new List<(string Title, List<List<string>> Table)>();
		var nextNode = parentNode;
		do
		{
			nextNode = nextNode.NextSibling;
			if (nextNode.Name != "table")
				continue;
			var title = nextNode.PreviousSibling.PreviousSibling.InnerText;
			subCategories.Add((title, ParseTable(nextNode)));
		} while (nextNode.Name != "h1" && nextNode.NextSibling != null);
		return (Title: parentNode.InnerText, SubCategories: subCategories);
	}
}