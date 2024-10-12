using System.Diagnostics.CodeAnalysis;
using Machinery;

namespace ResponsiveFlow;

using static TryHelpers;

public sealed partial class MainWindowViewModel
{
    internal abstract record State : IState<MainWindowViewModel, IEvent, State>
    {
        public abstract bool TryCreateNewState(MainWindowViewModel context, IEvent ev,
            [MaybeNullWhen(false)] out State newState);

        public void OnExiting(MainWindowViewModel context, IEvent ev, State newState) { }

        public void OnExited(MainWindowViewModel context, IEvent ev, State newState) { }

        public void OnRemain(MainWindowViewModel context, IEvent ev) { }

        public void OnEntering(MainWindowViewModel context, IEvent ev, State oldState) { }

        public void OnEntered(MainWindowViewModel context, IEvent ev, State oldState) { }
    }

    internal sealed record ProjectNotLoadedState : State
    {
        internal static ProjectNotLoadedState Instance { get; } = new();

        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState)
        {
            if (ev is not OpenEvent openEvent)
                return None(out newState);

            newState = new LoadingState(openEvent.ProjectPath);
            return true;
        }
    }

    internal sealed record LoadingState(string ProjectPath) : State
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState)
        {
            if (ev is not CompleteEvent completeEvent)
                return None(out newState);

            newState = new ReadyToRunState(ProjectPath, completeEvent.Project);
            return true;
        }
    }

    internal sealed record ReadyToRunState(string ProjectPath, ProjectDto Project) : State
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState)
        {
            if (ev is OpenEvent openEvent)
                return Some(new LoadingState(openEvent.ProjectPath), out newState);

            if (ev is not RunEvent)
                return None(out newState);

            newState = new RunningState(ProjectPath, Project);
            return true;
        }
    }

    internal sealed record RunningState(string ProjectPath, ProjectDto Project) : State
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState)
        {
            if (ev is not CompleteEvent completeEvent || Project != completeEvent.Project)
                return None(out newState);

            newState = new CompletedState(ProjectPath, Project);
            return true;
        }
    }

    internal sealed record CompletedState(string ProjectPath, ProjectDto Project) : State
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState)
        {
            if (ev is RunEvent)
                return Some(new RunningState(ProjectPath, Project), out newState);

            if (ev is not OpenEvent openEvent)
                return None(out newState);

            newState = new LoadingState(openEvent.ProjectPath);
            return true;
        }
    }
}
