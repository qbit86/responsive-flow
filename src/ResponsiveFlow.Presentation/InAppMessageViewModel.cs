using System;

namespace ResponsiveFlow;

public sealed class InAppMessageViewModel
{
    private readonly InAppMessage _inAppMessage;

    private InAppMessageViewModel(InAppMessage inAppMessage) => _inAppMessage = inAppMessage;

    public string Text => _inAppMessage.MessageOrException;

    internal static InAppMessageViewModel Create(InAppMessage inAppMessage)
    {
        ArgumentNullException.ThrowIfNull(inAppMessage);
        return new(inAppMessage);
    }
}
