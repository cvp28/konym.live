using CosmoSlim;
using Hv2UI;

namespace konym.live.textmode.UI;

internal class BadAppleLayer : Layer
{
	[Widget] DrawTool dtBadApple;

	public BadAppleLayer(Hv2App Instance, bool Active) : base(Instance, Active)
	{
		dtBadApple = new(BadAppleRender);

		AddWidgetsInternal();

		KeyActions.Add((ConsoleKey.Enter, ConsoleModifiers.None), cki =>
		{
			this.Active = false;
			Instance.Layers["Chat"].Active = true;
		});
	}

	private int f;

	private void BadAppleRender(Renderer r)
	{
		if (Globals.BadAppleLoadTask.Status == TaskStatus.Running)
		{
			r.WriteAt(2, 1, $"Server is still pre-loading frames, please be patient... ");

			r.WriteAt(2, 3, "Also, you can skip Bad Apple with [ENTER]");
			r.WriteAt(2, 4, "Though, why would you? :/");
		}
		else if (Globals.BadAppleLoadTask.Status == TaskStatus.RanToCompletion)
		{
			string frame = Globals.BadAppleFrames[f];

			string[] frame_lines = frame.Split('\n');

			for (int i = 0; i < frame_lines.Length; i++)
			{
				r.WriteAt(0, i, frame_lines[i]);

			}

			if (f < 120)
			{
				r.WriteAt(83, 1, "Press [ENTER]");
				r.WriteAt(83, 2, "to skip");
			}

			if (f == Globals.BadAppleFrames.Length - 1)
			{
				Active = false;
				Instance.Layers["Chat"].Active = true;
				return;
			}
			else
			{
				f++;
			}

			// intentionally slow the renderer down to meet ~60ish FPS;
			Thread.Sleep(1);
		}

	}
}