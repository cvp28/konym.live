using System.Diagnostics;

using CosmoSlim;
using Hv2UI;

namespace konym.live.textmode.UI;

internal class ChatLayer : Layer
{
	[Widget] public DrawTool dtOutput;
	[Widget] public InputField ifMsg;
	[Widget] public TextBox tbChat, tbActiveUsers;

	private string ID;

	public ChatLayer(string ID, Hv2App Instance, bool Active) : base(Instance, Active)
	{
		this.ID = ID;

		dtOutput = new(OnCustomRender);

		ifMsg = new()
		{
			X = 2,
			Y = 2,
			Prompt = "> ",

			CursorBlinkIntervalMs = 1000,

			OnInputReady = OnInput,
			ClearOnSubmit = true
		};

		tbChat = new(0, 4, 88, 24)
		{
			Visible = true,
			ScrollbarVisible = false
		};

		tbActiveUsers = new(90, 4, 28, 24)
		{
			Visible = true,
			ScrollbarVisible = false
		};

		AddWidgetsInternal();
	}

	private void OnInput(string Input)
	{
		var Elements = Input.Split([' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (Elements.Length == 0)
			return;

		string Command = Elements[0];

		switch (Command)
		{
			case "/bye":
			case "/cya":
			case "/goodbye":
			case "/farewell":
				Globals.DoChatMessage("System", $"{ID} has left the server", Color24.Green, Color24.Yellow);
				Instance.SignalExit();
				break;

			case "/badapple":
				Globals.DoChatMessage("System", $"{ID} is re-watching bad apple", Color24.Green, Color24.White);

				Instance.Layers["Chat"].Active = false;
				Instance.Layers["Main"] = new BadAppleLayer(Instance, true);
				break;

			case "/clear":
				tbChat.Clear();
				break;

			case "/help":
				tbChat.WriteLine("konym.live.textmode chat (ported to http/websockets!) - built 8/15/2026");
				tbChat.WriteLine();
				tbChat.WriteLine("Client commands are below!");
				tbChat.WriteLine();
				tbChat.WriteLine("/bye or /cya or /goodbye or /farewell");
				tbChat.WriteLine("= disconnect from the server");
				tbChat.WriteLine();
				tbChat.WriteLine("/badapple");
				tbChat.WriteLine("= replay bad apple intro");
				tbChat.WriteLine();
				tbChat.WriteLine("/clear");
				tbChat.WriteLine("= clear the chat window");
				tbChat.WriteLine();
				tbChat.WriteLine("/help");
				tbChat.WriteLine("= display this (obviously)");
				break;

			case "/uptime":
				var web_uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
				tbChat.WriteLine($"konym.live uptime: {web_uptime.Days} days, {web_uptime.Hours} hours, {web_uptime.Minutes} minutes");
				break;

			case "/whoami":
				tbChat.WriteLine($"You are {ID}", Color24.Green, Color24.Black);
				break;

			case "/kick":
				if (Elements.Length < 3)
				{
					tbChat.WriteLine("Please provide an ID and the password", Color24.Red, Color24.Black);
					break;
				}

				string User = Elements[1];
				string Pass = Elements[2];

				if (!Globals.Hv2Instances.ContainsKey(User))
				{
					tbChat.WriteLine($"User not found: {User}", Color24.Red, Color24.Black);
					break;
				}

				// this is really secure nigga
				if (Pass != "all4114all")
				{
					tbChat.WriteLine($"Incorrect password", Color24.Red, Color24.Black);
					break;
				}

				Globals.Hv2Instances[User].SignalExit();
				break;

			default:
				Globals.DoChatMessage(ID, Input, Color24.Yellow, Color24.White);
				break;
		}
	}

	private void OnCustomRender(Renderer r)
	{
		int random = Random.Shared.Next();

		r.WriteAt(2, 1, "Welcome to the anonymous chat! Please enter a message. Or don't. I don't really care what you do.");

		tbActiveUsers.Clear();

		foreach (var inst in Globals.Hv2Instances)
		{
			tbActiveUsers.Write($"{inst.Key} ", Color24.Yellow, Color24.Black);
			tbActiveUsers.Write($"{(inst.Value.Layers["Main"].Active ? $"in BadApple" : $"in Chat ")}", Color24.Green, Color24.Black);

			if (inst.Value == Instance)
				tbActiveUsers.WriteLine("(you)");
			else
				tbActiveUsers.WriteLine();
		}

		//	var fg = new byte[3];
		//	Random.Shared.NextBytes(fg);

		//	r.WriteAt((r.Width / 2) - 20, r.Height / 2, Run.Fg(fg[0], fg[1], fg[2]), $"Your random number is {Random.Shared.Next()}");

		//	Index += 1;
		//	Index %= LoadingChars.Length;
		//	
		//	r.WriteAt(0, 0, Color24.Aqua, $"{LoadingChars[Index]}");
	}

	public override void OnShow()
	{
		base.OnShow();
		Instance.FocusedWidget = ifMsg;
	}
}
