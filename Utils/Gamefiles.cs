using System;
using System.IO;

public class GameFiles
{
    private static string _userDirectoryPath;

    public static string UserDirectoryPath
    {
        get
        {
            if (_userDirectoryPath != null)
                return _userDirectoryPath;

            var path = _isUnixPlatform()
                ? Path.Combine(_getHomeDirectory(), ".local", "share", "Hackfest")
                : Path.Combine(_getHomeDirectory(), "Documents", "MyGames", "Hackfest");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            _userDirectoryPath = path;
            return path;
        }
    }

    private static string _getHomeDirectory()
    {
        return _isUnixPlatform()
            ? Environment.GetEnvironmentVariable("HOME")
            : Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
    }

    private static bool _isUnixPlatform()
    {
        var p = (int) Environment.OSVersion.Platform;
        return p == 4 || p == 6 || p == 128;
    }
}