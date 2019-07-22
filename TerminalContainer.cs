using Godot;

public class TerminalContainer : Control
{
    public Terminal Terminal;

    public TerminalContainer()
    {
        Visible = false;
        Terminal = new Terminal(GetFont("arial"), RectSize);
    }

    public void Open(ComputerContainer container)
    {
        var bridge = GetNode<BridgeContainer>("/VM Bridge Manager");
        bridge.VagrantBridge.AttachToComputer(container.Computer, out var stdin, out var stdout, out var stderr, true);
        Terminal.Open(stdin, stdout);
    }

    public void Close()
    {
        Terminal.Close();
    }
    
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

        if (!Visible)
            return;
        
        if (@event is InputEventKey key)
        {
            if (key.Unicode == (int) KeyList.Escape)
            {
                Close();
                return;
            }
            
            Terminal.OnInput((char) key.Unicode);
        }
    }
}