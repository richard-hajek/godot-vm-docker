public static class ASCII
{
    /// <summary>
    ///     Null character
    /// </summary>
    public const int NUL = 0;

    /// <summary>
    ///     Start Of Heading
    /// </summary>
    public const int SOH = 1;

    /// <summary>
    ///     Start of Text
    /// </summary>
    public const int STX = 2;

    /// <summary>
    ///     End of Text
    /// </summary>
    public const int ETX = 3;

    /// <summary>
    ///     End of Transmission
    /// </summary>
    public const int EOT = 4;

    /// <summary>
    ///     enquiry
    /// </summary>
    public const int ENQ = 5;

    /// <summary>
    ///     Acknowledge
    /// </summary>
    public const int ACK = 6;

    /// <summary>
    ///     Bell
    /// </summary>
    public const int BEL = 7;

    /// <summary>
    ///     Backspace
    /// </summary>
    public const int BS = 8;

    /// <summary>
    ///     Horizontal tab
    /// </summary>
    public const int TAB = 9;

    /// <summary>
    ///     New line
    /// </summary>
    public const int LF = 10;

    /// <summary>
    ///     Vertical tab
    /// </summary>
    public const int VT = 11;

    /// <summary>
    ///     New page
    /// </summary>
    public const int FF = 12;

    /// <summary>
    ///     Carriage return
    /// </summary>
    public const int CR = 13;

    /// <summary>
    ///     Shift out
    /// </summary>
    public const int SO = 14;

    /// <summary>
    ///     Shift in
    /// </summary>
    public const int SI = 15;

    /// <summary>
    ///     Data link escape
    /// </summary>
    public const int DLE = 16;

    /// <summary>
    ///     Device Control 1
    /// </summary>
    public const int DC1 = 17;

    /// <summary>
    ///     Device Control 2
    /// </summary>
    public const int DC2 = 18;

    /// <summary>
    ///     Device Control 3
    /// </summary>
    public const int DC3 = 19;

    /// <summary>
    ///     Device Control 4
    /// </summary>
    public const int DC4 = 20;

    /// <summary>
    ///     Negative Acknowledge
    /// </summary>
    public const int NAK = 21;

    /// <summary>
    ///     Synchronous Idle
    /// </summary>
    public const int SYN = 22;

    /// <summary>
    ///     End of transmission block
    /// </summary>
    public const int ETB = 23;

    /// <summary>
    ///     Cancel
    /// </summary>
    public const int CAN = 24;

    /// <summary>
    ///     End of medium
    /// </summary>
    public const int EM = 25;

    /// <summary>
    ///     Substitute
    /// </summary>
    public const int SUB = 26;

    /// <summary>
    ///     Escape
    /// </summary>
    public const int ESC = 27;

    /// <summary>
    ///     File separator
    /// </summary>
    public const int FS = 28;

    /// <summary>
    ///     Group separator
    /// </summary>
    public const int GS = 29;

    /// <summary>
    ///     Record separator
    /// </summary>
    public const int RS = 30;

    /// <summary>
    ///     Unit separator
    /// </summary>
    public const int US = 31;

    /// <summary>
    ///     Space
    /// </summary>
    public const int SPACE = 32;

    /// <summary>
    ///     Delete
    /// </summary>
    public const int DEL = 127;

    public static bool IsAlphabet(int c)
    {
        if (65 <= c && c <= 90) // Check for codes A-Z
            return true;

        if (97 <= c && c <= 122) // Check for codes a-z
            return true;

        return false;
    }

    public static bool IsPrintable(char c)
    {
        return 32 <= c && c <= 126;
    }
}