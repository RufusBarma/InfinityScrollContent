using ContentAggregator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContentAggregator.CategoriesAggregators.Reddit;

public class RedditCsvCategoriesEnricher
{
	private readonly ILogger<RedditCsvCategoriesEnricher> _logger;
	private readonly string _csvDirPath;

	public RedditCsvCategoriesEnricher(ILogger<RedditCsvCategoriesEnricher> _logger, IConfiguration config)
	{
		this._logger = _logger;
		_csvDirPath = config["Reddit:CsvDirPath"];
		if (string.IsNullOrEmpty(_csvDirPath) || !Directory.Exists(_csvDirPath))
			_logger.LogWarning("Reddit:CsvDirPath is empty or not exist");
	}

	public async Task<IEnumerable<CategoryItem>> Enrich(IEnumerable<CategoryItem> categories)
	{
		if (string.IsNullOrEmpty(_csvDirPath) || !Directory.Exists(_csvDirPath))
			return categories;
		var categoriesList = categories.ToList();
		foreach (var file in new DirectoryInfo(_csvDirPath).GetFiles())
		{
			using var reader = file.OpenText();
			while (!reader.EndOfStream)
			{
				var row = (await reader.ReadLineAsync())?.Split(',');
				if (row == null || row.Length < 2)
					break;
				var subReddit = "/r/" + row[0];
				var additionalCategory = row[1];
				var categoryIndex = categoriesList.FindIndex(category =>
					string.Compare(category.Title, subReddit, StringComparison.InvariantCultureIgnoreCase) == 0 &&
					!category.Group.Contains(additionalCategory, StringComparer.InvariantCultureIgnoreCase));

				if (categoryIndex < 0)
					continue;
				var categoryItem = categoriesList[categoryIndex];
				var groups = categoryItem.Group.ToList();
				groups.Insert(1, additionalCategory);
				categoriesList[categoryIndex] = categoryItem with {Group = groups.ToArray()};
			}
		}
		return categoriesList;
	}
}