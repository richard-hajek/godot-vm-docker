using System.IO;
using System.Text;
using Godot;

public class TerminalContainer : Control
{
    public Terminal Terminal;

    public TerminalContainer()
    {
        Visible = false;
        Terminal = new Terminal(GetFont("arial"), RectSize);
        Terminal.ScreenUpdated += Update;
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
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, RectSize), Colors.Black);

        short pointerX = 0;
        short pointerY = 0;
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
                
            if (key.Unicode == (int) KeyList.Escape)
            {
                Close();
                return;
            }

            var ascii = Encoding.ASCII.GetChars(new[] {(byte) key.Unicode});
            
            Terminal.OnInput(ascii[0]);
        }
    }
}