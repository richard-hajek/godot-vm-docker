using System.Collections.Generic;
using System.IO;
using Godot;
using Thread = System.Threading.Thread;

[Tool]
public class Terminal
{
    private int _currentAttributes;
    private readonly Color _currentBackground = Colors.Black;
    private readonly Color _currentForeground = Colors.White;

    private short _cursorX;
    private short _cursorY;

    private StreamWriter _stdin;
    private StreamReader _stdout;

    private readonly short _termSizeX;
    private readonly short _termSizeY;

    public Font Font;

    public List<Line> Lines = new List<Line>();

    public Node Parent;

    public Terminal(Font font, Vector2 termSize)
    {
        Font = font;
        _termSizeX = (short) (termSize.x / FontSizeX);
        _termSizeY = (short) (termSize.y / FontSizeY);

        Lines.Add(new Line {Columns = new Glyph[_termSizeX]});
    }

    public float FontSizeX => Font.GetStringSize("X").x;
    public float FontSizeY => Font.GetStringSize("X").y;

    public void Open(StreamWriter stdin, StreamReader stdout)
    {
        _stdin = stdin;
        _stdout = stdout;

        var thread = new Thread(streamRead);
        thread.Start(stdout);
    }

    public void Close()
    {
    }

    private void streamRead(object o)
    {
        var reader = (StreamReader) o;
        while (reader != null && !reader.EndOfStream)
        {
            var c = reader.Read();
            OnOutput((char) c);
        }
    }

    public void OnOutput(char c)
    {
        if (!ASCII.IsPrintable(c))
        {
            switch (c)
            {
                case '\n':
                case '\r':
                    _cursorMove((short) -_cursorX, 1, false, true);
                    break;
            }

            return;
        }

        Lines[_cursorY].Columns[_cursorX] = new Glyph
        {
            Character = c,
            Attributes = _currentAttributes,
            ForegroundColor = _currentForeground,
            BackgroundColor = _currentBackground
        };

        _cursorMove(1, 0, true, true);
    }

    public void OnInput(char c)
    {
        _stdin.Write(c);
    }

    private void _cursorMove(short dx, short dy, bool wrap = false, bool scroll = false)
    {
        var newX = _cursorX + dx;
        var newY = _cursorY + dy;

        if (newX >= Lines[_cursorY].Columns.Length)
        {
            if (wrap)
            {
                newX = 0;
                newY++;
            }
            else
            {
                newX = Lines[_cursorY].Columns.Length - 1;
            }
        }

        var linesCount = Lines.Count;
        if (newY >= linesCount)
        {
            Lines.Add(new Line {Columns = new Glyph[_termSizeX]});
            linesCount++;

            if (linesCount > _termSizeY)
            {
                if (scroll)
                {
                    Lines.RemoveAt(0);
                    linesCount--;
                }
                else
                {
                    Lines.RemoveAt(linesCount - 1);
                    linesCount--;
                }

                newY = linesCount - 1;
            }
        }

        _cursorX = (short) newX;
        _cursorY = (short) newY;
    }

    internal struct Glyph
    {
        internal char Character;
        internal int Attributes;
        internal Color BackgroundColor;
        internal Color ForegroundColor;
    }

    public struct Line
    {
        internal Glyph[] Columns;
    }
}