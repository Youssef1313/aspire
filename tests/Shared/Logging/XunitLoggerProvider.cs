// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Microsoft.Extensions.Logging.Testing;

public class MSTestLoggerProvider : ILoggerProvider
{
    private readonly TestContext _output;
    private readonly LogLevel _minLevel;
    private readonly DateTimeOffset? _logStart;

    public MSTestLoggerProvider(TestContext output)
        : this(output, LogLevel.Trace)
    {
    }

    public MSTestLoggerProvider(TestContext output, LogLevel minLevel)
        : this(output, minLevel, null)
    {
    }

    public MSTestLoggerProvider(TestContext output, LogLevel minLevel, DateTimeOffset? logStart)
    {
        _output = output;
        _minLevel = minLevel;
        _logStart = logStart;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new MSTestLogger(_output, categoryName, _minLevel, _logStart);
    }

    public void Dispose()
    {
    }
}

public class MSTestLogger : ILogger
{
    private static readonly string[] s_newLineChars = new[] { Environment.NewLine };
    private readonly string _category;
    private readonly LogLevel _minLogLevel;
    private readonly TestContext _output;
    private readonly DateTimeOffset? _logStart;

    public MSTestLogger(TestContext output, string category, LogLevel minLogLevel, DateTimeOffset? logStart)
    {
        _minLogLevel = minLogLevel;
        _category = category;
        _output = output;
        _logStart = logStart;
    }

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Buffer the message into a single string in order to avoid shearing the message when running across multiple threads.
        var messageBuilder = new StringBuilder();

        var timestamp = _logStart.HasValue ?
            $"{(DateTimeOffset.UtcNow - _logStart.Value).TotalSeconds.ToString("N3", CultureInfo.InvariantCulture)}s" :
            DateTimeOffset.UtcNow.ToString("s", CultureInfo.InvariantCulture);

        var firstLinePrefix = $"| [{timestamp}] {_category} {logLevel}: ";
        var lines = formatter(state, exception).Split(s_newLineChars, StringSplitOptions.RemoveEmptyEntries);
        messageBuilder.AppendLine(firstLinePrefix + lines.FirstOrDefault() ?? string.Empty);

        var additionalLinePrefix = "|" + new string(' ', firstLinePrefix.Length - 1);
        foreach (var line in lines.Skip(1))
        {
            messageBuilder.AppendLine(additionalLinePrefix + line);
        }

        if (exception != null)
        {
            lines = exception.ToString().Split(s_newLineChars, StringSplitOptions.RemoveEmptyEntries);
            additionalLinePrefix = "| ";
            foreach (var line in lines)
            {
                messageBuilder.AppendLine(additionalLinePrefix + line);
            }
        }

        // Remove the last line-break, because TestContext only has WriteLine.
        var message = messageBuilder.ToString();
        if (message.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            message = message.Substring(0, message.Length - Environment.NewLine.Length);
        }

        try
        {
            _output.WriteLine(message);
        }
        catch (Exception)
        {
            // We could fail because we're on a background thread and our captured TestContext is
            // busted (if the test "completed" before the background thread fired).
            // So, ignore this. There isn't really anything we can do but hope the
            // caller has additional loggers registered
        }
    }

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= _minLogLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => new NullScope();

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
