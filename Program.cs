using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;

using konym.live.Pages;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<ConcurrentBag<User>>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.UseForwardedHeaders();

// Active Users endpoint
// Returns simple string
app.MapGet("/active-users", (HttpContext ctx, ConcurrentBag<User> UserList) =>
{
	PingActive(ctx.Connection.RemoteIpAddress, ctx.Request.Path, UserList, SetWhere: false);
	var UserCount = UserList.Count(user => user.Valid);

	return $"""<span class="badge">🌎 {(UserCount > 1 ? $"{UserCount} visits" : $"{UserCount} visit")}, {UserList.Count(user => Stopwatch.GetElapsedTime(user.LastPing).Seconds < 30)} online now!</span>""";
});

// Pages
app.MapGet("/", (HttpContext ctx, ConcurrentBag<User> UserList) =>
{
	PingActive(ctx.Connection.RemoteIpAddress, ctx.Request.Path, UserList);

	RenderFragment Index = builder =>
	{
		builder.OpenComponent<IndexPage>(0);
		builder.CloseComponent();
	};

	return new RazorComponentResult<MainLayout>(new { Content = Index });
});

app.MapGet("/about", (HttpContext ctx, ConcurrentBag<User> UserList) =>
{
	PingActive(ctx.Connection.RemoteIpAddress, ctx.Request.Path, UserList);

	RenderFragment About = builder =>
	{
		builder.OpenComponent<AboutPage>(0);
		builder.CloseComponent();
	};

	return new RazorComponentResult<MainLayout>(new { Content = About });
});

app.MapGet("/download", (HttpContext ctx, ConcurrentBag<User> UserList) =>
{
	PingActive(ctx.Connection.RemoteIpAddress, ctx.Request.Path, UserList);

	RenderFragment Download = builder =>
	{
		builder.OpenComponent<DownloadPage>(0);
		builder.CloseComponent();
	};

	return new RazorComponentResult<MainLayout>(new { Content = Download });
});


app.Run();

static async void PingActive(IPAddress RemoteAddress, string Path, ConcurrentBag<User> ActiveUsers, bool SetWhere = true)
{
	var TryUser = ActiveUsers.FirstOrDefault(user => user.IP.Equals(RemoteAddress));

	if (TryUser is null)
	{
		// get country using ip-api
		using HttpClient client = new HttpClient();
		var Country = await client.GetStringAsync($"http://ip-api.com/csv/{RemoteAddress}?fields=1", CancellationToken.None);

		Console.WriteLine($"{RemoteAddress} :: {Country}");

		// Add new visit to the tracker
		var visit = new User
		{
			IP = RemoteAddress,
			Country = Country,
			Where = Path,
			LastPing = Stopwatch.GetTimestamp(),

			// Don't consider this visit valid until the user has pinged at least once, to avoid counting bots and scrapers
			Valid = false
		};

		ActiveUsers.Add(visit);
	}
	else
	{
		TryUser.LastPing = Stopwatch.GetTimestamp();
		TryUser.Valid = true;

		// Avoids setting user's location to a backend API endpoint
		if (SetWhere)
			TryUser.Where = Path;

		Console.WriteLine($"{TryUser.IP} :: Alive as of {TryUser.LastPing} at {TryUser.Where}");
	}
}

public class User
{
	public IPAddress IP;
	public string Country;
	public string Where;
	public long LastPing;
	public bool Valid;
}

//	public class ActiveUsersMiddleware
//	{
//		private readonly RequestDelegate _next;
//		public readonly ConcurrentBag<User> ActiveUsers;
//	
//		// Inject the singleton service into the constructor
//		public ActiveUsersMiddleware(RequestDelegate next, ConcurrentBag<User> ActiveUsers)
//		{
//			_next = next;
//			this.ActiveUsers = ActiveUsers;
//		}
//	
//		public async Task InvokeAsync(HttpContext context)
//		{
//			
//	
//		end:
//			await _next(context);
//		}
//	}