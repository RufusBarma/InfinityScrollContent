using ContentAggregator.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace ContentAggregator.CategoriesAggregators.Reddit;

public class RedditHtmlCategoriesAggregator
{
	private readonly List<string> _headers = Enumerable.Range(1, 6).Select(level => "h" + level).ToList();
	private readonly string _categoriesAddress;

	public RedditHtmlCategoriesAggregator(IConfiguration config)
	{
		_categoriesAddress = config["Reddit:CategoriesAddress"];
	}

	public async Task<IEnumerable<CategoryItem>> GetCategories()
	{
		return await ParseCategories(_categoriesAddress);;
	}

	private bool IsHeader(HtmlNode node) => _headers.Contains(node.Name);

	private int CompareHeader(HtmlNode left, HtmlNode right)
	{
		var leftDeep = int.Parse(left.Name.Substring(1));
		var rightDeep = int.Parse(right.Name.Substring(1));
		return leftDeep.CompareTo(rightDeep);
	}

	private async Task<IEnumerable<CategoryItem>> ParseCategories(string address)
	{
		var page = await new HttpClient().GetStringAsync(address);

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

	private IEnumerable<CategoryItem> GetCategories(List<HtmlNode> nodes)
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