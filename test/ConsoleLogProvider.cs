using App.Metrics.Logging;

namespace test;

public class ConsoleLogProvider : ILogProvider
{
    private readonly LogLevel _minLogLevel;

    public ConsoleLogProvider(LogLevel minLogLevel = LogLevel.Trace)
    {
        _minLogLevel = minLogLevel;
    }

    public Logger GetLogger(string name)
    {
        return new ConsoleLogger(name, _minLogLevel).Log;
    }

    public IDisposable OpenNestedContext(string message)
    {
        throw new NotImplementedException();
    }

    public IDisposable OpenMappedContext(string key, string value)
    {
        throw new NotImplementedException();
    }
}

public class ConsoleLogger
{
    private readonly string _loggerName;
    private readonly LogLevel _minLogLevel;

    public ConsoleLogger(string loggerName, LogLevel minLogLevel)
    {
        _loggerName = loggerName;
        _minLogLevel = minLogLevel;
    }

    private ConsoleColor MapLevelToColor(LogLevel level)
    {
        return level switch {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.Magenta,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }

    private readonly object _lock = new Object();

    public bool Log(
        LogLevel logLevel,
        Func<string>? messageFunc,
        Exception? exception = null,
        params object[] formatParameters
    ) {
        if (messageFunc == null) {
            return logLevel >= _minLogLevel;
        }

        var message = messageFunc();

        lock (_lock) {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = MapLevelToColor(logLevel);
            Console.WriteLine($"{DateTime.Now:T} [{logLevel}] {_loggerName}:\n{message}\n");
            Console.ForegroundColor = defaultColor;
            if (exception != null) {
                Console.WriteLine(exception.ToString());
            }
        }

        return true;
    }
}
