using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed class InAppMessage
{
    private InAppMessage(LogLevel level, string? message, Exception? exception)
    {
        Debug.Assert(message is not null || exception is not null);
        Level = level;
        Message = message;
        Exception = exception;
    }

    public LogLevel Level { get; }

    public string? Message { get; }

    public Exception? Exception { get; }

    public string MessageOrEmpty => Message ?? string.Empty;

    public string MessageOrException => Message ?? Exception!.Message;

    public static InAppMessage FromMessage(string message, LogLevel level = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new(level, message, null);
    }

    public static InAppMessage FromException(
        Exception exception, string? message = null, LogLevel level = LogLevel.Error)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(level, message, exception);
    }

    public static InAppMessage FromException(Exception exception, LogLevel level = LogLevel.Error)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(level, null, exception);
    }

    public bool TryGetMessage([NotNullWhen(true)] out string? message)
    {
        message = Message;
        return message is not null;
    }

    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return exception is not null;
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(InAppMessage));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Level = ").Append(Level);
        if (Message is { } message)
            builder.Append(", Message = ").Append(message);
        if (Exception is { } exception)
            builder.Append(", Exception.Message = ").Append(exception.Message);
        return true;
    }
}
