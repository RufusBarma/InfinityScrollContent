using System.Net;
using ContentAggregator.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ContentAggregator.Aggregators.Reddit;

public class RedditCategoriesAggregator
{
	private readonly List<string> _headers = Enumerable.Range(1, 6).Select(level => "h" + level).ToList();
	private readonly string _cachePath;
	private readonly string _categoriesAddress;
	private readonly string _categoriesPagePath;
	private MongoClient _clientDb;

	public RedditCategoriesAggregator(IConfiguration config)
	{
		_cachePath = config["Cache:CategoriesPath"];
		_categoriesAddress = config["Reddit:CategoriesAddress"];
		_categoriesPagePath = config["Cache:RootPath"] + "pageCache.txt";
		_clientDb = new MongoClient(config.GetConnectionString("DefaultConnection"));
	}

	public List<CategoryItem> GetCategories()
	{
		var database = _clientDb.GetDatabase("Reddit");
		var collection = database.GetCollection<CategoryItem>("Categories");
		var documentsCount = collection.CountDocuments(doc => true);
		var categories = collection.Aggregate().ToList();
		if (documentsCount == 0)
		{
			categories = ParseCategories(_categoriesAddress);
			collection.InsertMany(categories);
		}
		return categories;
	}

	private bool IsHeader(HtmlNode node) => _headers.Contains(node.Name);

	private int CompareHeader(HtmlNode left, HtmlNode right)
	{
		var leftDeep = int.Parse(left.Name.Substring(1));
		var rightDeep = int.Parse(right.Name.Substring(1));
		return leftDeep.CompareTo(rightDeep);
	}

	private List<CategoryItem> ParseCategories(string address)
	{
		string page;
		if (!File.Exists(_categoriesPagePath))
		{
			var webClient = new WebClient();
			page = webClient.DownloadString(address);
			new FileInfo(_cachePath).Directory?.Create();
			File.WriteAllText(_categoriesPagePath, page);
		}
		else
		{
			page = File.ReadAllText(_categoriesPagePath);
		}

		var doc = new HtmlDocument();
		doc.LoadHtml(page);
		doc.OptionEmptyCollection = true;
		var filter = new List<string>(_headers) {"table"};
		var nodes = doc.DocumentNode
			.SelectSingleNode("//div[contains(@class, 'md') and contains(@class, 'wiki')]")
			.ChildNodes
			.Where(node => filter.Contains(node.Name))
			.Skip(1)
			.ToList();

		return GetCategories(nodes);
	}

	private List<CategoryItem> GetCategories(List<HtmlNode> nodes)
	{
		var categories = new List<CategoryItem>();
		var previousNodes = new Stack<(HtmlNode Node, string Category)>();
		foreach (var node in nodes)
			if (IsHeader(node))
			{
				while (previousNodes.Count != 0 && CompareHeader(node, previousNodes.Peek().Node) <= 0)
					previousNodes.Pop();
				previousNodes.Push((node, node.InnerText));
			}
			else if (node.Name == "table")
			{
				var previousCategory = previousNodes.Select(previous => previous.Category).Reverse().ToArray();
				categories.AddRange(GetCategoryItems(node, previousCategory));
			}

		return categories;
	}

	private List<List<string>> ParseTable(HtmlNode tableNode)
	{
		return tableNode
			.Descendants("tr")
			.Skip(1)
			.Where(tr => tr.Elements("td").Count() > 1)
			.Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
			.ToList();
	}

	private IEnumerable<CategoryItem> GetCategoryItems(HtmlNode table, string[] group) => GetCategoryItems(ParseTable(table), group);

	private IEnumerable<CategoryItem> GetCategoryItems(List<List<string>> table, string[] group) =>
		table.Select(row => new CategoryItem
		{
			Title = row.First(),
			Description = row.Last(),
			Group = group
		});
}