using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class UriRunner
{
    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
        Message = "An exception occurred while calling the '{MemberName}' member.")]
    partial void LogException(Exception exception, [CallerMemberName] string memberName = "");
}
