using Godot;

[Tool]
public class TerminalContainer : Control
{
    public Terminal Terminal;

    public override void _EnterTree()
    {
        Terminal = new Terminal(GetFont("arial"), RectSize);
        Terminal.OnOutput('h');
        Terminal.OnOutput('e');
        Terminal.OnOutput('l');
        Terminal.OnOutput('l');
        Terminal.OnOutput('o');
        Terminal.OnOutput('\n');
        Terminal.OnOutput('o');
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

        if (@event is InputEventKey key) Terminal.OnInput((char) key.Unicode);
    }
}