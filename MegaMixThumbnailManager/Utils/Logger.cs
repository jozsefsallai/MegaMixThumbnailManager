using System;

internal static class Logger
{
    private static readonly string PREFIX = "[MegaMixThumbnailManager]";
    
    private static readonly string WARNING = "[WARNING]";
    private static readonly string ERROR = "[ERROR]";

    public static void Log(object message)
    {
        Console.WriteLine($"{PREFIX} {message}");
    }

    public static void Warning(object message)
    {
        Log($"{WARNING} {message}");
    }

    public static void Error(object message)
    {
        Log($"{ERROR} {message}");
    }

    public static void Error(Exception ex)
    {
        Error(ex.Message);
    }
}