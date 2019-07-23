using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Thread = System.Threading.Thread;

[Tool]
public class Terminal
{

    public static Color DefaultBackground = Colors.Black;
    public static Color DefaultForeground = Colors.White;
    
    private int _currentAttributes;
    private Color _currentBackground = DefaultBackground;
    private Color _currentForeground = DefaultForeground;

    private int _cursorX;
    private int _cursorY;

    private StreamWriter _stdin;
    private StreamReader _stdout;

    private readonly int _termSizeX;
    private readonly int _termSizeY;

    public Font Font;

    public List<Line> Lines = new List<Line>();

    public Node Parent;

    public event ScreenUpdatedDelegate ScreenUpdated;

    public Terminal(Font font, Vector2 termSize)
    {
        Font = font;
        _termSizeX =  (int) (termSize.x / FontSizeX);
        _termSizeY = (int) (termSize.y / FontSizeY);

        Lines.Add(new Line {Columns = new Glyph[_termSizeX]});
    }

    public float FontSizeX => Font.GetStringSize("X").x;
    public float FontSizeY => Font.GetStringSize("X").y;

    private EscapeStates _escapeState = EscapeStates.NoEscape;
    private List<int> _escapeArguments = new List<int>();
    private string _argumentInWriting = "";
    private string _escapeSequence = "";
    
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
        if (_processEscape(c))
        {
            ScreenUpdated?.Invoke();
            return;
        }
    
        if (!ASCII.IsPrintable(c))
        {
            switch (c)
            {
                case '\n':
                    _cursorMove(0, 1, false, true);
                    break;
                case '\r':
                    _cursorX = 0;
                    break;
                case (char) ASCII.BS:
                    Lines[_cursorY].Columns[_cursorX] = new Glyph();
                    _cursorMove(-1, 0);
                    break;
                case (char) ASCII.DEL:
                    Lines[_cursorY].Columns[_cursorX] = new Glyph();
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

        ScreenUpdated?.Invoke();
    }
    
    /// <summary>
    /// Processes the next character, inspecting for CSI sequences
    /// </summary>
    /// <param name="c">Next character</param>
    /// <returns>True if the character was consumed</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private bool _processEscape(char c)
    {
        switch (_escapeState)
        {
            case EscapeStates.NoEscape:
                if (c == ASCII.ESC)
                {
                    _escapeState = EscapeStates.EscapeSequence;
                    _escapeSequence += c;
                    return true;
                }

                return false;
            case EscapeStates.EscapeSequence:

                _escapeSequence += c;
                if (c == '[')
                {
                    _escapeState = EscapeStates.CSISequence;
                    return true;
                }
                else
                {
                    GD.Print($"Executing ESC {_escapeSequence}");
                    _executeEscNonCSI(c);
                    _escapeState = EscapeStates.NoEscape;
                    return true;
                }
            case EscapeStates.CSISequence:

                _escapeSequence += c;
                
                if (int.TryParse(c + "", out _))
                {
                    _argumentInWriting += c;
                    return true;
                }
                else if (c == ';')
                {

                    if (_argumentInWriting.Length != 0)
                    {
                        _escapeArguments.Add(int.Parse(_argumentInWriting));
                        _argumentInWriting = "";
                    }
                    
                    return true;
                }
                else
                {
                    
                    if (_argumentInWriting.Length != 0)
                    {
                        _escapeArguments.Add(int.Parse(_argumentInWriting));
                        _argumentInWriting = "";
                    }
                    
                    GD.Print($"Executing CSI {_escapeSequence}");
                    _executeCSI( c, _escapeArguments);
                    
                    _escapeArguments.Clear();
                    _escapeSequence = "";
                    
                    _escapeState = EscapeStates.NoEscape;
                    return true;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    private void _executeEscNonCSI(char command)
    {
        var unsupported = false;
        
        switch (command)
        {
            case 'c': // Reset
                _cursorX = 0;
                _cursorY = 0;
                _currentAttributes = 0;
                _currentBackground = DefaultBackground;
                _currentForeground = DefaultForeground;
                Lines.Clear();
                Lines.Add(new Line {Columns = new Glyph[_termSizeX]});
                break;
            case 'D': // Linefeed
                _cursorMove(0, 1, false, true);
                break;
            case 'E': // Newline
                _cursorMove(0, 1, false, true);
                _cursorX = 0;
                break;
            case 'H': // Set tab stop at current column
                unsupported = true;
                break;
            case 'M': // Reverse linefeed
                _cursorMove(0, -1, false, true);
                break;
            case '7': // Save current state
                unsupported = true;
                break;
            case '8': // Restore state
                unsupported = true;
                break;
            case '[': // CSI sequence introducer - should not be handler here
                throw new Exception("Bug occured in Terminal.cs, Esc executer was called to execute CSI sequence");
            case '(': // Starts sequence defining G0 character set
                unsupported = true;
                break;
            case ')': // Starts sequence defining G1 char set
                unsupported = true;
                break;
            case '>': // Set numeric keypad mode
                unsupported = true;
                break;
            case '=': // Set application keypad mode
                unsupported = true;
                break;
            case ']': // Operation system command
                unsupported = true;
                break;
            default:
                GD.Print($"Esc Execute encountered an unknown ESC sequence: '{_escapeSequence}'");
                break;
        }
        
        if (unsupported)
            GD.Print($"Esc Executer encountered a unsupported sequence: '{_escapeSequence}'");
    }
    private readonly Dictionary<int, Color> bit16Colors = new Dictionary<int, Color>
    {
        {0, Colors.Black},
        {1, Colors.Red},
        {2, Colors.Green},
        {3, Colors.Brown},
        {4, Colors.Blue},
        {5, Colors.Magenta},
        {6, Colors.Cyan},
        {7, Colors.White}
    };
    private void _executeCSI(char command, List<int> arguments)
    {

        var unsupported = false;
        var badSyntax = false;
        
        var argsNum = arguments.Count;

        switch (command)
        {
            case '@': // Insert # of blank characters
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }

                var blanks = arguments[0];
                _cursorMove( blanks, 0, true, true);
                break;
            case 'A': // Move the cursor up # of rows

                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                var rows = arguments[0];
                _cursorMove(0, -rows, false, true);
            
                break;
            case 'B': // Move the cursor down # of rows
                
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                rows = arguments[0];
                _cursorMove(0, rows, false, true);
                
                break;
            case 'C': // Move the cursor right # of columns
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                var columns = arguments[0];
                _cursorMove (columns, 0, false, true);
                
                break;
            case 'D': // Move the cursor left # of columns
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                columns = arguments[0];
                _cursorMove( -columns, 0, false, true);
                
                break;
            case 'E': // Move the cursor down # of rows, to column 1
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                rows = arguments[0];
                _cursorMove(0, rows, false, true);
                _cursorX = 0;
                
                break;
            case 'F': // Move the cursor up # of rows, to column 1
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                rows = arguments[0];
                _cursorMove(0,  -rows, false, true);
                _cursorX = 0;
                break;
            case 'G': // Move to the indicated column in the current row
                
                if (argsNum != 1)
                {
                    badSyntax = true;
                    break;
                }
                
                var column = arguments[0];
                _cursorX = column;
                break;
            case 'H': // Move to the indicated row, column

                int row;
                if (argsNum == 0)
                {
                    row = 0;
                    column = 0;
                }
                else if (argsNum == 2)
                {
                    row = arguments[0];
                    column = arguments[1];
                }
                else
                {
                    badSyntax = true;
                    break;
                }

                _cursorY = row;
                _cursorX = column;
                
                break;
            case 'J': // Erase display

                int mode;

                if (argsNum == 0)
                {
                    mode = 0;
                }
                else if (argsNum == 1)
                {
                    mode = arguments[0];
                }
                else
                {
                    badSyntax = true;
                    break;
                }

                switch (mode)
                {
                    case 0: // From cursor to end
                        if (_cursorY + 1 < Lines.Count)
                        {
                            Lines.RemoveRange(_cursorY + 1, Lines.Count - _cursorY - 1);
                        }

                        for (var x = _cursorX; x < _termSizeX; x++)
                        {
                            Lines[_cursorY].Columns[x] = new Glyph();
                        }
                        break;
                    case 1: // From start to cursor
                        unsupported = true;
                        break;
                    case 2: // Whole display
                    case 3: // Whole display, including scroll back
                        Lines.Clear();
                        Lines.Add(new Line {Columns = new Glyph[_termSizeX]});
                        break;
                    default:
                        badSyntax = true;
                        break;
                }
                break;
            case 'K': // Erase line
                
                if (argsNum == 0)
                {
                    mode = 0;
                }
                else if (argsNum == 1)
                {
                    mode = arguments[0];
                }
                else
                {
                    badSyntax = true;
                    break;
                }

                switch (mode)
                {
                    case 0: // From cursor to end
                        unsupported = true;
                        break;
                    case 1: // From start to cursor
                        unsupported = true;
                        break;
                    case 2: // Whole line
                        Lines[_cursorY] = new Line{Columns = new Glyph[_termSizeX]};
                        break;
                    default:
                        badSyntax = true;
                        break;
                }
                
                break;
            case 'L': // Insert # of blank lines
                unsupported = true;
                break;
            case 'M': // Delete # of lines
                unsupported = true;
                break;
            case 'P': // Delete # of characters in the current line
                unsupported = true;
                break;
            case 'X': // Erase the indicated # in the current line
                unsupported = true;
                break;
            case 'a': // Move cursor right # of columns
                unsupported = true;
                break;
            case 'c': // Answer ESC
                unsupported = true;
                break;
            case 'd': // Move cursor to # row, current column
                unsupported = true;
                break;
            case 'e': // Move cursor down the indicated # of rows
                unsupported = true;
                break;
            case 'f': // Move the cursor to the indicated row, column
                unsupported = true;
                break;
            case 'g': // Clear tab stop at current position
                unsupported = true;
                break;
            case 'h': // Set mode
                unsupported = true;
                break;
            case 'l': // Reset mode
                unsupported = true;
                break;
            case 'm': // Set attributes

                if (argsNum == 0)
                {
                    _currentBackground = DefaultBackground;
                    _currentForeground = DefaultForeground;
                    _currentAttributes = 0;
                    break;
                }
                        
                foreach (var arg in arguments)
                {
                    
                    if (30 <= arg && arg <= 37)
                    {
                        var n = arg - 30;
                        var color = bit16Colors[n];
                        _currentForeground = color;
                    }
                    else if (40 <= arg && arg <= 47)
                    {
                        var n = arg - 40;
                        var color = bit16Colors[n];
                        _currentBackground = color;
                    } else switch (arg)
                    {
                        case 0:
                            _currentBackground = DefaultBackground;
                            _currentForeground = DefaultForeground;
                            _currentAttributes = 0;
                            break;
                        case 38:
                        case 39:
                            _currentForeground = DefaultForeground;
                            break;
                        case 49:
                            _currentBackground = DefaultBackground;
                            break;
                        default: // Assume that we are not supporting color, instead of assuming that it doens't exist
                            GD.Print($"Unknown graphics mode: {arg}");
                            break;
                    }
                }
                break;
            case 'n': // Status report
                unsupported = true;
                break;
            case 'q': // Set Keyboard LEDS
                unsupported = true;
                break;
            case 'r': // Set scrolling region
                unsupported = true;
                break;
            case 's': // Save cursor location
                unsupported = true;
                break;
            case 'u': // Load cursor location
                unsupported = true;
                break;
            case '`': // Move cursor to the indicated column in the current row
                unsupported = true;
                break;
            default:
                GD.Print($"CSI Execute encountered an unknown sequence: '{_escapeSequence}'");
                break;
        }
        
        if (unsupported)
            GD.Print($"CSI Executer encountered a unsupported sequence: '{_escapeSequence}'");
        
        if (badSyntax)
            GD.Print($"CSI Executer encountered a sequence with bad syntax: '{_escapeSequence}'");
    }

    public void OnInput(char c)
    {
        _stdin.Write(c);
    }

    private void _cursorMove(int dx, int dy, bool wrap = false, bool scroll = false)
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

        _cursorX =  newX;
        _cursorY =  newY;
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

    public delegate void ScreenUpdatedDelegate();

    internal enum EscapeStates
    {
        NoEscape,
        EscapeSequence,
        CSISequence
    }
}