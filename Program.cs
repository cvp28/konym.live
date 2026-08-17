using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.HttpResults;

using Spectre.Console;
using Utf8StringInterpolation;

using CosmoSlim;
using Hv2UI;

using konym.live;
using konym.live.Pages;
using konym.live.Pages.Zones;
using konym.live.Pages.Testing;
using konym.live.Components;
using konym.live.textmode.UI;

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
app.UseForwardedHeaders();
app.UseWebSockets();

app.UseStaticFiles(new StaticFileOptions()
{
	FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
	RequestPath = ""
});

app.MapStaticAssets();

/*
app.MapGet("/bedroom-light-on", (HttpContext ctx) =>
{
	using var client = new UdpClient("wiz_bc4853.lan", 38899);

	var bytes = Encoding.UTF8.GetBytes("""{"id":1,"method":"setState","params":{"state":true}}""");
	client.Send(bytes, bytes.Length);
});

app.MapGet("/bedroom-light-off", (HttpContext ctx) =>
{
	using var client = new UdpClient("wiz_bc4853.lan", 38899);

	var bytes = Encoding.UTF8.GetBytes("""{"id":1,"method":"setState","params":{"state":false}}""");
	client.Send(bytes, bytes.Length);
});
*/

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
			$"{user.LastPing:d} {user.LastPing:t}",
			user.Valid.ToString()
		);

	foreach (var user in InactiveUsers)
		table.AddRow(
			"[red]INACTIVE[/]",
			user.IP.ToString(),
			user.Country,
			user.Where,
			$"{user.LastPing:d} {user.LastPing:t}",
			user.Valid.ToString()
		);

	AnsiConsole.Write(table);

	return $"""<span id="badge">🌎 {(TotalVisits > 1 ? $"{TotalVisits} visits" : $"{TotalVisits} visit")}, {ActiveUsers.Count()} online now!</span>""";
});

app.MapGet("/frag/random-song", (HttpContext ctx) =>
{
	return Globals.RandomMp3File;
});

app.MapGet("/frag/get-server-power-info", (HttpContext ctx) =>
{
	var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
	Kernel32.GetSystemPowerStatus(out var status);

	return
	$"""
	{uptime.TotalHours:F0} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds<br />
	{(status.BatteryLifePercent != 255 ? $"{status.BatteryLifePercent}%" : "N/A")}<br />
	{(status.ACLineStatus == 1 ? "Yes" : "No")}
	""";
});

app.MapGet("/frag/mp3-list", (HttpContext ctx) =>
{
	StringBuilder content = new();

	//return string.Join(',', Globals.Mp3Files.ToArray());
	return new RazorComponentResult<Mp3List>();
});

app.Map("/chat-ws", async (HttpContext ctx) =>
{
	if (!ctx.WebSockets.IsWebSocketRequest)
	{
		ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
		return;
	}

	using var socket = await ctx.WebSockets.AcceptWebSocketAsync();

	var hv2 = new Hv2App(120, 30, true);

	hv2.Layers.Add("Main", new BadAppleLayer(hv2, true));
	hv2.Layers.Add("Chat", new ChatLayer(ctx.Connection.Id, hv2, false));
	
	Globals.Hv2Instances[ctx.Connection.Id] = hv2;

	var InputTaskCanceller = new CancellationTokenSource();

	// Input from client task
	var InputTask = Task.Run(async delegate
	{
		byte[] InputBuffer = new byte[1024];
		WebSocketReceiveResult recv;

		do
		{
			try
			{
				recv = await socket.ReceiveAsync(new(InputBuffer), InputTaskCanceller.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			var rcvd = Encoding.UTF8.GetString(InputBuffer, 0, recv.Count);

			ConsoleKeyInfo cki;

			switch (rcvd)
			{
				case "\e[A":
					// up arrow
					cki = new(' ', ConsoleKey.UpArrow, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					break;

				case "\e[B":
					cki = new(' ', ConsoleKey.DownArrow, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					continue;

				case "\e[C":
					cki = new(' ', ConsoleKey.RightArrow, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					break;

				case "\e[D":
					cki = new(' ', ConsoleKey.LeftArrow, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					break;

				case "\u007f":
					cki = new(' ', ConsoleKey.Backspace, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					break;

				case "\r\n":
				case "\r":
				case "\n":
					cki = new(' ', ConsoleKey.Enter, false, false, false);
					Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					break;

				default:
					foreach (char c in rcvd)
					{
						cki = new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false);
						Globals.Hv2Instances[ctx.Connection.Id].InputBuffer.Enqueue(cki);
					}
					break;
			}
		}
		while (recv.MessageType != WebSocketMessageType.Close);
	});

	// Output to client task
	hv2.OnRendererOutput = async delegate (Utf8StringBuffer buf)
	{
		if (socket.State != WebSocketState.Open)
		{
			//Console.WriteLine($"{ctx.Connection.Id} renderer running even though connection is closed, signalling exit...");
			Globals.Hv2Instances[ctx.Connection.Id].SignalExit();
			return;
		}

		try
		{
			var arr = buf.ToArray();
			await socket.SendAsync(new(arr), WebSocketMessageType.Text, true, CancellationToken.None);

			//Console.WriteLine($"Sent {arr.Length} bytes to {ctx.Connection.Id}");
		}
		catch (Exception)
		{
			//Console.WriteLine("Error writing to client {0}: {1}", ctx.Connection.Id, ex.Message);
			Globals.Hv2Instances[ctx.Connection.Id].SignalExit();
		}
	};

	hv2.Run();

	// Cleanup input task
	InputTaskCanceller.Cancel();
	await InputTask;

	// Cleanup Hv2 instance
	Globals.Hv2Instances.Remove(ctx.Connection.Id, out _);

});

// Simple pages
app.MapGet("/", APIUtils.DefaultPartialOrFullHandler<IndexPage>);
app.MapGet("/about", APIUtils.DefaultPartialOrFullHandler<AboutPage>);
app.MapGet("/zones", APIUtils.DefaultPartialOrFullHandler<ZonesPage>);
app.MapGet("/i-made-this", APIUtils.DefaultPartialOrFullHandler<IMadeThisPage>);
app.MapGet("/shadez-2-full", APIUtils.DefaultPartialOrFullHandler<Shadez2FullPage>);

// Hidden pages
app.MapGet("/test-page", APIUtils.DefaultPartialOrFullHandler<TestPage>);
app.MapGet("/nothing-yet", APIUtils.DefaultPartialOrFullHandler<NothingYetPage>);

// Complicated pages
app.MapGet("/zones/technology", APIUtils.ZoneHandler<TechnologyZonePage>);
app.MapGet("/zones/life", APIUtils.ZoneHandler<LifeZonePage>);
app.MapGet("/zones/cars-and-bikes", APIUtils.ZoneHandler<CarsAndBikesZonePage>);

Globals.Mp3FilesRefreshTask = Task.Run(Globals.Mp3FilesRefreshProc);
Globals.BadAppleLoadTask = Task.Run(Globals.LoadBadAppleFrames);

app.Run();

static async void PingActive(IPAddress RemoteAddress, IHeaderDictionary Headers, ConcurrentBag<User> ActiveUsers)
{
	var TryUser = ActiveUsers.FirstOrDefault(user => user.IP.Equals(RemoteAddress));

	if (TryUser is null)
	{
		using var client = new HttpClient();
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

public static class Globals
{
	public static string RandomMp3File => Mp3Files.ElementAt(new Random().Next(Mp3Files.Count));
	public static ConcurrentBag<string> Mp3Files = [""];
	public static Task Mp3FilesRefreshTask;

	public static string[] BadAppleFrames;
	public static Task BadAppleLoadTask;

	public static ConcurrentDictionary<string, Hv2App> Hv2Instances = [];

	public static async void Mp3FilesRefreshProc()
	{
		while (true)
		{
			Mp3Files.Clear();
			var files = Directory.GetFiles(@".\wwwroot\mp3\");

			foreach (var file in files)
				Mp3Files.Add(Path.GetFileName(file));

			await Task.Delay(5000);
		}
	}

	public static void LoadBadAppleFrames()
	{
		//var FramePaths = Directory.GetFiles(@".\ServerContent\bad_apple_frames_ascii");
		var FramePaths = Directory.GetFiles(@"D:\konym.live\TextModeApp\bad_apple_frames_ascii");
		BadAppleFrames = new string[FramePaths.Length];

		for (int i = 0; i < FramePaths.Length; i++)
			BadAppleFrames[i] = File.ReadAllText(FramePaths[i]);
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public static void DoChatMessage(string From, string Message, Color24 FromFG, Color24 MessageFG)
	{
		foreach (var kvp in Hv2Instances)
		{
			(kvp.Value.Layers["Chat"] as ChatLayer).tbChat.Write($"{From}", FromFG, Color24.Black);
			(kvp.Value.Layers["Chat"] as ChatLayer).tbChat.WriteLine($": {Message}", MessageFG, Color24.Black);
		}
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

public static partial class Kernel32
{
	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetSystemPowerStatus(out SystemPowerStatus sps);

	[StructLayout(LayoutKind.Sequential)]
	public struct SystemPowerStatus
	{
		public byte ACLineStatus;
		public byte BatteryFlag;
		public byte BatteryLifePercent;
		public byte Reserved1;
		public int BatteryLifeTime;
		public int BatteryFullLifeTime;
	}
}