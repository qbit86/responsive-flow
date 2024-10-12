namespace ResponsiveFlow;

internal interface IEvent;

internal sealed class RunEvent : IEvent
{
    internal static RunEvent Instance { get; } = new();
}

internal sealed record OpenEvent(string ProjectPath) : IEvent;

internal sealed record CompleteEvent(ProjectDto Project) : IEvent;
