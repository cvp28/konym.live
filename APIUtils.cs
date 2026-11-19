using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;

using Htmx;

using konym.live.Pages;
using konym.live.Pages.Zones;
using konym.live.Components.Zones;

namespace konym.live;

public static class APIUtils
{
	public readonly static Dictionary<Zone, string> ZoneRoutes = new()
	{
		{ Zone.Technology, "/zones/technology" },
		{ Zone.Life, "/zones/life" },
		{ Zone.CarsAndBikes, "/zones/cars-and-bikes" }
	};

	public readonly static Dictionary<Zone, string> ZoneServerTopicPaths = new()
	{
		{ Zone.Technology, @".\ServerContent\tech-topics" },
		{ Zone.Life, @".\ServerContent\life-topics" },
		{ Zone.CarsAndBikes, @".\ServerContent\carsandbikes-topics" }
	};

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
		var ZoneVal = typeof(T) switch
		{
			var type when type == typeof(TechnologyZonePage) => Zone.Technology,
			var type when type == typeof(LifeZonePage) => Zone.Life,
			var type when type == typeof(CarsAndBikesZonePage) => Zone.CarsAndBikes,

			_ => throw new Exception("Invalid zone page")
		};

		if (ctx.Request.IsHtmx())
		{
			if (ctx.TryGetDate(out var Date))
			{
				if (!TryGetTopicHtml(ZoneVal, Date, out var TopicHtml))
					return null;

				return new RazorComponentResult<ArticleView>(new { TopicHtml, ZoneVal });
			}
			else
			{
				RenderFragment ZoneRootContent = b =>
				{
					b.OpenComponent<ZoneRootContent>(0);
					b.AddComponentParameter(1, "HasArticle", false);
					b.AddComponentParameter(2, "ZoneVal", ZoneVal);
					b.CloseComponent();
				};

				return new RazorComponentResult<T>(new { ZoneRootContent });
			}
		}
		else
		{
			RenderFragment Content = b =>
			{
				b.OpenComponent<T>(0);
				b.AddComponentParameter(1, "ZoneRootContent", (RenderFragment) (b2 =>
				{
					b2.OpenComponent<ZoneRootContent>(0);

					// Inline logic and queries in the RenderTreeBuilder block...
					// this is probably against the rules, but it makes the code smaller, so ¯\_(ツ)_/¯

					if (ctx.TryGetDate(out var Date))
					{
						b2.AddComponentParameter(1, "HasArticle", TryGetTopicHtml(ZoneVal, Date, out var TopicHtml));
						b2.AddComponentParameter(2, "TopicHtml", TopicHtml);
						b2.AddComponentParameter(3, "ZoneVal", ZoneVal);
					}
					else
					{
						b2.AddComponentParameter(1, "HasArticle", false);
						b2.AddComponentParameter(2, "ZoneVal", ZoneVal);
					}

					b2.CloseComponent();

				}));
				b.CloseComponent();
			};

			return new RazorComponentResult<MainLayout>(new { Content });
		}
	}

	public static bool TryGetDate(this HttpContext ctx, out DateOnly Date, string QueryParam = "date")
	{
		Date = default;
		return ctx.Request.Query.TryGetValue(QueryParam, out var ParamVal) && DateOnly.TryParse(ParamVal, out Date);
	}

	public static List<(string Title, DateOnly Date)> GetTopics(Zone Zone)
	{
		// The following code isn't particularly good... but it works....... for now.
		List<(string Title, DateOnly Date)> Topics = [];

		var TopicFiles = Directory.GetFiles(ZoneServerTopicPaths[Zone], "*.md");

		foreach (var TopicFile in TopicFiles)
		{
			var TopicFileName = Path.GetFileNameWithoutExtension(TopicFile);
			var Elements = TopicFileName.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (Elements.Length != 2)
				continue;

			string Title = Elements[0];

			if (!DateOnly.TryParse(Elements[1], out var Date))
				continue;

			Topics.Add((Title, Date));
		}

		Topics.Sort((t1, t2) => t2.Date.CompareTo(t1.Date));
		return Topics;
	}

	public static bool TryGetTopicHtml(Zone Zone, DateOnly Date, out MarkupString TopicHtml)
	{
		var TopicFile = Directory.GetFiles(ZoneServerTopicPaths[Zone], $"*_{Date:MM-dd-yyyy}.md").FirstOrDefault();

		if (TopicFile is null)
			goto empty;

		try
		{
			var TopicMd = File.ReadAllText(TopicFile);
			TopicHtml = (MarkupString) Markdig.Markdown.ToHtml(TopicMd);

			return true;
		}
		catch (Exception ex)
		{
			using var ExceptionLog = File.Open(@".\ExceptionLog.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			using var sw = new StreamWriter(ExceptionLog);

			sw.WriteLine("-------------------------------------------");
			sw.WriteLine($"New exception: {DateTime.Now}");
			sw.WriteLine(ex.Message + "\n");
			sw.WriteLine(ex.StackTrace);
			sw.Flush();

			goto empty;
		}

	empty:
		TopicHtml = (MarkupString) string.Empty;
		return false;
	}
}

public enum Zone
{
	Technology,
	Life,
	CarsAndBikes
}
