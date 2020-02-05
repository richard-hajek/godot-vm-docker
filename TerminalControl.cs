using System.Collections.Generic;
using Godot;

public class TerminalControl : Control
{
	private readonly List<int> _modifierKeys = new List<int>
	{
		(int) KeyList.Shift,
		(int) KeyList.Alt,
		(int) KeyList.Control,
		(int) KeyList.Capslock,
		(int) KeyList.Numlock,
		(int) KeyList.Scrolllock
	};

	private readonly Dictionary<ContainerNode, Terminal> _terminals = new Dictionary<ContainerNode, Terminal>();

	private Terminal _currentTerminal;

	public Font Font;

	public TerminalControl()
	{
		Visible = false;
		Font = GetFont("monospace");
	}

	public float FontSizeX => Font.GetStringSize("X").x + 1;
	public float FontSizeY => Font.GetStringSize("X").y + 2;

	public void Open(ContainerNode container)
	{
		if (BridgeNode.DryMode)
		{
			GD.PrintErr("Dry Mode active, refusing to open a terminal.");
			return;
		}

		if (_terminals.ContainsKey(container))
		{
			_currentTerminal = _terminals[container];
			Visible = true;
			return;
		}

		BridgeNode.ContainerApi.CreateTTY(container.Id, out var stdin, out var stdout, true);

		_currentTerminal = new Terminal((int) (RectSize.x / FontSizeX), (int) (RectSize.y / FontSizeY));
		_currentTerminal.ScreenUpdated += Update;
		_currentTerminal.Open(stdin, stdout);

		_terminals.Add(container, _currentTerminal);

		Visible = true;
	}

	public void Close()
	{
		Visible = false;
		_currentTerminal = null;
	}

	public override void _EnterTree()
	{
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(0, 0, RectSize), Colors.Black);

		short pointerX = 0;
		short pointerY = 0;

		for (var y = 0; y < _currentTerminal.Lines.Count; y++)
		{
			var line = _currentTerminal.Lines[y];

			pointerX = 0;

			for (var x = 0; x < line.Columns.Length; x++)
			{
				var glyph = line.Columns[x];

				var posX = pointerX * FontSizeX;
				var posY = pointerY * FontSizeY;

				DrawRect(new Rect2(posX, posY, FontSizeX, FontSizeY), glyph.BackgroundColor);
				DrawString(Font, new Vector2(posX, posY + FontSizeY), glyph.Character + "", glyph.ForegroundColor);
				pointerX++;
			}

			pointerY++;
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (!Visible)
			return;

		if (@event is InputEventKey key)
		{
			if (key.Echo)
				return;

			if (!key.Pressed)
				return;

			if (key.Scancode == (int) KeyList.Escape && key.Shift)
			{
				Close();
				return;
			}

			if (_modifierKeys.Contains((int)(key.Scancode)))
				return;

			var character = (char) key.Unicode;

			if (key.Scancode == (int) KeyList.Enter)
				character = (char) ASCII.LF;

			if (key.Scancode == (int) KeyList.Backspace)
				character = (char) ASCII.BS;

			if (key.Scancode == (int) KeyList.Delete)
				character = (char) ASCII.DEL;

			if (key.Scancode == (int) KeyList.Escape)
				character = (char) ASCII.ESC;

			if (key.Scancode == (int) KeyList.C && key.Control)
				character = (char) ASCII.ETX;
			
			if (key.Scancode == (int) KeyList.D && key.Control)
				character = (char) ASCII.EOT;

			_currentTerminal.OnInput(character);
		}
	}
}
