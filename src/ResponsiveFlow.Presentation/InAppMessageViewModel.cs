using System;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed class InAppMessageViewModel
{
    private readonly InAppMessage _inAppMessage;

    public InAppMessageViewModel() : this(InAppMessage.FromMessage(string.Empty), Brushes.Transparent) { }

    private InAppMessageViewModel(InAppMessage inAppMessage, Brush backgroundBrush)
    {
        _inAppMessage = inAppMessage;
        BackgroundBrush = backgroundBrush;
    }

    public Brush BackgroundBrush { get; }

    public string Text => _inAppMessage.MessageOrException;

    internal static InAppMessageViewModel Create(InAppMessage inAppMessage)
    {
        ArgumentNullException.ThrowIfNull(inAppMessage);
        Brush backgroundBrush = inAppMessage.Level switch
        {
            LogLevel.Information => Brushes.Honeydew,
            LogLevel.Warning => Brushes.Beige,
            LogLevel.Error or LogLevel.Critical => Brushes.LavenderBlush,
            _ => Brushes.Transparent
        };
        return new(inAppMessage, backgroundBrush);
    }
}
