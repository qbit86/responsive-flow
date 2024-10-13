using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel
{
    [LoggerMessage(EventId = 5, Level = LogLevel.Error,
        Message = "An exception occurred while calling the '{MemberName}' member.")]
    partial void LogException(Exception exception, [CallerMemberName] string memberName = "");
}
