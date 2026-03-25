using System.Net;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.HttpOverrides;

using Spectre.Console;

using konym.live;
using konym.live.Pages;
using konym.live.Pages.Zones;
using konym.live.Pages.Testing;

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

app.MapPost("/frag/active-users", (HttpContext ctx, ConcurrentBag<User> UserList) =>
{
	PingActive(ctx.Connection.RemoteIpAddress, ctx.Request.Headers, UserList);

	AnsiConsole.Clear();
	AnsiConsole.WriteLine("/frag/active-users\n");

	var table = new Table();
	table.AddColumns("Status", "IP Address", "Country", "URL", "Last Ping", "Valid");

	var TotalVisits = UserList.Count(user => user.Valid);
	var ActiveUsers = UserList.Where(user => user.Active);
	var InactiveUsers = UserList.Where(user => !user.Active);

	foreach (var user in ActiveUsers)
		table.AddRow(
			"[green]ACTIVE[/]",
			user.IP.ToString(),
			user.Country,
			user.Where,
			$"{user.LastPing.ToShortDateString()} {user.LastPing.ToShortTimeString()}",
			user.Valid.ToString()
		);

	foreach (var user in InactiveUsers)
		table.AddRow(
			"[red]INACTIVE[/]",
			user.IP.ToString(),
			user.Country,
			user.Where,
			$"{user.LastPing.ToShortDateString()} {user.LastPing.ToShortTimeString()}",
			user.Valid.ToString()
		);

	AnsiConsole.Write(table);

	return $"""<span id="badge">🌎 {(TotalVisits > 1 ? $"{TotalVisits} visits" : $"{TotalVisits} visit")}, {ActiveUsers.Count()} online now!</span>""";
});

// Simple pages
app.MapGet("/", APIUtils.DefaultPartialOrFullHandler<IndexPage>);
app.MapGet("/about", APIUtils.DefaultPartialOrFullHandler<AboutPage>);
app.MapGet("/zones", APIUtils.DefaultPartialOrFullHandler<ZonesPage>);
app.MapGet("/i-made-this", APIUtils.DefaultPartialOrFullHandler<IMadeThisPage>);

// Hidden pages
app.MapGet("/test-page", APIUtils.DefaultPartialOrFullHandler<TestPage>);
app.MapGet("/nothing-yet", APIUtils.DefaultPartialOrFullHandler<NothingYetPage>);

// Complicated pages
app.MapGet("/zones/technology", APIUtils.ZoneHandler<TechnologyZonePage>);
app.MapGet("/zones/life", APIUtils.ZoneHandler<LifeZonePage>);
app.MapGet("/zones/cars-and-bikes", APIUtils.ZoneHandler<CarsAndBikesZonePage>);

app.Run();

static async void PingActive(IPAddress RemoteAddress, IHeaderDictionary Headers, ConcurrentBag<User> ActiveUsers)
{
	var TryUser = ActiveUsers.FirstOrDefault(user => user.IP.Equals(RemoteAddress));

	if (TryUser is null)
	{
		using HttpClient client = new HttpClient();
		string Country = Headers.TryGetValue("CF-IPCountry", out var country) ? country : "<unknown>";

		// Add new visit to the tracker
		var visit = new User
		{
			IP = RemoteAddress,
			Country = Country,
			Where = Headers.TryGetValue("Hx-Current-Url", out var where) ? where : "/",
			LastPing = DateTime.Now,

			// Don't consider this visit valid until the user has pinged at least once, to avoid counting bots and scrapers
			Valid = false
		};

		ActiveUsers.Add(visit);
	}
	else
	{
		TryUser.LastPing = DateTime.Now;
		TryUser.Valid = true;
		TryUser.Where = Headers.TryGetValue("Hx-Current-Url", out var where) ? where : "/";
	}
}

public class User
{
	public IPAddress IP;
	public string Country;
	public string Where;
	public DateTime LastPing;
	public bool Valid;

	public bool Active => Valid && (DateTime.Now - LastPing).TotalSeconds < 30;
}