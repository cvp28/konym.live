using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Hv2UI;
using CosmoSlim;

namespace konym.live.textmode.UI;

internal class ChatLayer : Layer
{
	[Widget] public DrawTool dtOutput;
	[Widget] public InputField ifMsg;
	[Widget] public TextBox tbChat, tbActiveUsers;

	public static ConcurrentDictionary<string, ChatUser> Users;

	private string ID;

	private static readonly string[] PromptMessages = ["Start talkin'!", "Introduce yourself!", "Say hi!", "Speak up!", "We don't bite :p", "Care to share?", "Let us know what you think!"];

	public ChatLayer(string ID, Hv2App Instance, bool Active) : base(Instance, Active)
	{
		this.ID = ID;

		dtOutput = new(OnCustomRender)
		{
			//Visible = false
		};

		ifMsg = new()
		{
			//Visible = false,
			EmptyMessage = PromptMessages[Random.Shared.Next(PromptMessages.Length)],

			X = 2,
			Y = 2,
			Prompt = "> ",

			CursorBlinkIntervalMs = 1000,

			OnInputReady = OnInput,
			ClearOnSubmit = true
		};

		tbChat = new(0, 4, 88, 24)
		{
			//Visible = false,
			ScrollbarVisible = false
		};

		tbActiveUsers = new(90, 4, 28, 24)
		{
			//Visible = false,
			ScrollbarVisible = false
		};

		AddWidgetsInternal();
	}

	private void OnInput(string Input)
	{
		// Randomize playful input prompt
		ifMsg.EmptyMessage = PromptMessages[Random.Shared.Next(PromptMessages.Length)];

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
				DoChatMessage("System", $"{ID} has left the server", Color24.Green, Color24.Yellow);
				Instance.SignalExit();
				break;

			case "/badapple":
				DoChatMessage("System", $"{ID} is re-watching bad apple", Color24.Green, Color24.White);

				Instance.Layers["Chat"].Active = false;
				Instance.Layers["Main"] = new BadAppleLayer(Instance, true);
				break;

			case "/clear":
				tbChat.Clear();
				break;

			case "/help":
				tbChat.WriteLine("textmode.konym.live chat (ported to http/websockets!) - built 9/9/2026");
				tbChat.WriteLine();
				tbChat.WriteLine("Client commands are below!");
				tbChat.WriteLine();
				tbChat.WriteLine("/bye or /cya or /goodbye or /farewell");
				tbChat.WriteLine("= disconnect from the server");
				tbChat.WriteLine();
				tbChat.WriteLine("/badapple");
				tbChat.WriteLine("= replay bad apple intro");
				tbChat.WriteLine();
				tbChat.WriteLine("/whoami");
				tbChat.WriteLine("= prints your current name");
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
				DoChatMessage("System", $"{User} was kicked", Color24.Green, Color24.Yellow);
				break;

			default:
				DoChatMessage(ID, Input, Color24.Yellow, Color24.White);
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
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public static void DoChatMessage(string From, string Message, Color24 FromFG, Color24 MessageFG)
	{
		foreach (var kvp in Globals.Hv2Instances)
		{
			(kvp.Value.Layers["Chat"] as ChatLayer).tbChat.Write($"{From}", FromFG, Color24.Black);
			(kvp.Value.Layers["Chat"] as ChatLayer).tbChat.WriteLine($": {Message}", MessageFG, Color24.Black);
		}
	}

	public override void OnShow()
	{
		base.OnShow();
		Instance.FocusedWidget = ifMsg;
	}
}

public class ChatUser
{
	public Hv2App Hv2Instance;

	public string IP;

	public string Nickname;
	public string Room;

	public bool IsAdmin;
}