using System;

namespace Zero.Core;

public class Logging
{
    public LogLevel MinimumLogLevel;

    public string LogFileName;

    public void Clear()
    {
        Console.Clear();
    }

    public void WriteLine(string Line)
    {
        WriteLine(Line, LogLevel.Information);
    }

    public void WriteLine(string Line, LogLevel Level)
    {
        if (Level >= MinimumLogLevel)
        {
            SetLogColor(Level);
            Console.WriteLine(Line);
            ResetLogColor();
        }
    }

    private void SetLogColor(LogLevel Level)
    {
        switch (Level)
        {
            default:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogLevel.novouser:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case LogLevel.index:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }
    }

    private void ResetLogColor()
    {
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
