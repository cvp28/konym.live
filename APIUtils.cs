using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;

using Htmx;

using konym.live.Pages;
using konym.live.Pages.Zones;
using konym.live.Components.Zones;

namespace konym.live;

public static class APIUtils
{
	public static dynamic DefaultPartialOrFullHandler<T>(HttpContext ctx) where T : IComponent
	{
		if (ctx.Request.IsHtmx())
		{
			return new RazorComponentResult<T>();
		}
		else
		{
			RenderFragment Content = builder =>
			{
				builder.OpenComponent<T>(0);
				builder.CloseComponent();
			};

			return new RazorComponentResult<MainLayout>(new { Content });
		}
	}

	public static dynamic ZoneHandler<T>(HttpContext ctx) where T : IComponent
	{
		string ZoneName = typeof(T) switch
		{
			var type when type == typeof(TechnologyZonePage) => "technology",
			var type when type == typeof(LifeZonePage) => "life",
			var type when type == typeof(CarsAndBikesZonePage) => "carsandbikes"
		};

		if (ctx.Request.IsHtmx())
		{
			if (ctx.Request.Query.TryGetValue("date", out var date) && DateOnly.TryParse(date, out _))
			{
				var zone = ZoneName switch
				{
					"technology" => Zone.Technology,
					"life" => Zone.Life,
					"carsandbikes" => Zone.CarsAndBikes
				};

				if (!TryGetTopicHtml(zone, date, out var TopicHtml))
					return null;

				return new RazorComponentResult<ArticleView>(new { TopicHtml = new MarkupString(TopicHtml), ZoneName });
			}
			else
			{
				RenderFragment ZoneRootContent = builder =>
				{
					builder.OpenComponent<ZoneRootContent>(0);
					builder.AddComponentParameter(1, "HasArticle", false);
					builder.AddComponentParameter(2, "ZoneName", ZoneName);
					builder.CloseComponent();
				};

				return new RazorComponentResult<T>(new { ZoneRootContent });
			}
		}
		else
		{
			var Content = (RenderFragment) (builder =>
			{
				builder.OpenComponent<T>(0);
				builder.AddComponentParameter(1, "ZoneRootContent", (RenderFragment) (builder2 =>
				{
					builder2.OpenComponent<ZoneRootContent>(0);

					// Inline logic and queries in the RenderTreeBuilder block...
					// this is probably against the rules, but it makes the code smaller, so ¯\_(ツ)_/¯

					if (ctx.Request.Query.TryGetValue("date", out var date) && DateOnly.TryParse(date, out _))
					{
						builder2.AddComponentParameter(1, "HasArticle", TryGetTopicHtml(Zone.Technology, date, out var TopicHtml));
						builder2.AddComponentParameter(2, "TopicHtml", new MarkupString(TopicHtml));
						builder2.AddComponentParameter(3, "ZoneName", ZoneName);
					}
					else
					{
						builder2.AddComponentParameter(1, "HasArticle", false);
						builder2.AddComponentParameter(2, "ZoneName", ZoneName);
					}

					builder2.CloseComponent();

				}));
				builder.CloseComponent();
			});

			return new RazorComponentResult<MainLayout>(new { Content });
		}
	}

	public static List<(string Title, string Date)> GetTopics(string ZoneName)
	{
		// The following code isn't particularly good... but it works....... for now.
		List<(string Title, string Date)> Topics = [];

		var SearchPath = ZoneName switch
		{
			"technology" => @".\ServerContent\tech-topics",
			"life" => @".\ServerContent\life-topics",
			"carsandbikes" => @".\ServerContent\carsandbikes-topics"
		};

		foreach (var TopicFile in Directory.GetFileSystemEntries(SearchPath).Where(fse => fse.EndsWith(".md")))
		{
			var Elements = TopicFile.Split(['\\', '_', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			string Title = Elements[^3];
			string Date = Elements[^2];

			Topics.Add((Title, Date));
		}

		Topics.Sort((t1, t2) => t2.Date.CompareTo(t1.Date));

		return Topics;
	}

	public static bool TryGetTopicHtml(Zone Zone, string DateStr, out string TopicHtml)
	{
		string SearchPath = Zone switch
		{
			Zone.Technology => @".\ServerContent\tech-topics",
			Zone.Life => @".\ServerContent\life-topics",
			Zone.CarsAndBikes => @".\ServerContent\carsandbikes-topics",
			_ => string.Empty
		};

		var TopicFile = Directory.GetFileSystemEntries(SearchPath).FirstOrDefault(fse => fse.EndsWith($"{DateStr}.md"));

		if (TopicFile == null)
		{
			TopicHtml = string.Empty;
			return false;
		}

		try
		{
			var TopicMd = File.ReadAllText(TopicFile);
			TopicHtml = Markdig.Markdown.ToHtml(TopicMd);

			return true;
		}
		catch (Exception ex)
		{
			using var ExceptionFile = File.Open(@".\Exception_Log.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			using var sw = new StreamWriter(ExceptionFile);

			sw.WriteLine("-------------------------------------------");
			sw.WriteLine($"New exception: {DateTime.Now}");
			sw.WriteLine(ex.Message + "\n");
			sw.WriteLine(ex.StackTrace);
			sw.Flush();

			TopicHtml = string.Empty;
			return false;
		}
	}
}

public enum Zone
{
	Technology,
	Life,
	CarsAndBikes
}
