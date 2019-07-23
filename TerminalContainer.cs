using System.Collections.Generic;
using System.IO;
using Godot;

public class TerminalContainer : Control
{
    public Terminal Terminal;
    
    private List<int> _modifierKeys = new List<int>
    {
        (int) KeyList.Shift,
        (int) KeyList.Alt,
        (int) KeyList.Control,
        (int) KeyList.Capslock,
        (int) KeyList.Numlock,
        (int) KeyList.Scrolllock
    };

    public TerminalContainer()
    {
        Visible = false;
    }

    public void Open(ComputerContainer computer)
    {
        computer.OpenTerminal(this);
    }

    public void Open(StreamWriter stdin, StreamReader stdout)
    {
        Visible = true;
        Terminal.Open(stdin, stdout);
    }

    public void Close()
    {
        Terminal.Close();
        Visible = false;
    }
    
    public override void _EnterTree()
    {
        Terminal = new Terminal(GetFont("arial"), RectSize);
        Terminal.ScreenUpdated += Update;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, RectSize), Colors.Black);

        short pointerX = 0;
        short pointerY = 0;
        lock (Terminal.Lines)
        {
            foreach (var line in Terminal.Lines)
            {
                pointerX = 0;
                foreach (var glyph in line.Columns)
                {

                    var posX = pointerX * Terminal.FontSizeX;
                    var posY = pointerY * Terminal.FontSizeY;

                    DrawRect(new Rect2(posX, posY, Terminal.FontSizeX, Terminal.FontSizeY), glyph.BackgroundColor);
                    DrawString(Terminal.Font, new Vector2(posX, posY + Terminal.FontSizeY), glyph.Character + "", glyph.ForegroundColor);
                    pointerX++;
                }

                pointerY++;
            }
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
            
            if (key.Scancode == (int) KeyList.Escape)
            {
                Close();
                return;
            }

            if (_modifierKeys.Contains(key.Scancode))
            {
                return;
            }
            
            GD.Print($"Accepting unicode char: {key.Unicode}");
            GD.Print($"With char: {(char) key.Unicode}");
            
            var character = (char) key.Unicode;

            if (key.Scancode == (int) KeyList.Enter)
            {
                character = (char) ASCII.LF;
            }

            if (key.Scancode == (int) KeyList.Backspace)
            {
                character = (char) ASCII.BS;
            }

            if (key.Scancode == (int) KeyList.Delete)
            {
                character = (char) ASCII.DEL;
            }
            
            Terminal.OnInput(character);
        }
    }
    
}