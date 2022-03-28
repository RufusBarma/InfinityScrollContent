using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditCategoriesAggregator
{
	private readonly List<string> _headers = Enumerable.Range(1, 6).Select(level => "h" + level).ToList();
	private readonly string _cachePath;
	private readonly string _categoriesAddress;
	private readonly string _categoriesPagePath;

	public RedditCategoriesAggregator(IConfiguration config)
	{
		_cachePath = config["Cache:CategoriesPath"];
		_categoriesAddress = config["Reddit:CategoriesAddress"];
		_categoriesPagePath = config["Cache:RootPath"] + "pageCache.txt";
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

	private bool IsHeader(HtmlNode node) => _headers.Contains(node.Name);

	private int CompareHeader(HtmlNode left, HtmlNode right)
	{
		var leftDeep = int.Parse(left.Name.Substring(1));
		var rightDeep = int.Parse(right.Name.Substring(1));
		return leftDeep.CompareTo(rightDeep);
	}

	private List<Category> ParseCategories(string address)
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

	private List<Category> GetCategories(List<HtmlNode> nodes)
	{
		var categories = new List<Category>();
		var previousNodes = new Stack<(HtmlNode Node, Category Category)>();
		foreach (var node in nodes)
			if (IsHeader(node))
			{
				var category = new Category {Title = node.InnerText};
				while (previousNodes.Count != 0 && CompareHeader(node, previousNodes.Peek().Node) <= 0)
					previousNodes.Pop();
				if (node.Name == "h1")
					categories.Add(category);
				else
					previousNodes.Peek().Category.SubCategories.Add(category);
				previousNodes.Push((node, category));
			}
			else if (node.Name == "table")
			{
				var previousCategory = previousNodes.Peek().Category;
				previousCategory.Items.AddRange(GetCategoryItems(node));
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

	private IEnumerable<CategoryItem> GetCategoryItems(HtmlNode table) => GetCategoryItems(ParseTable(table));

	private IEnumerable<CategoryItem> GetCategoryItems(List<List<string>> table) =>
		table.Select(row => new CategoryItem
		{
			Title = row.First(),
			Description = row.Last()
		});
}