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

        public virtual void OnExiting(MainWindowViewModel context, IEvent ev, State newState) { }

        public virtual void OnExited(MainWindowViewModel context, IEvent ev, State newState) { }

        public void OnRemain(MainWindowViewModel context, IEvent ev) { }

        public void OnEntering(MainWindowViewModel context, IEvent ev, State oldState) { }

        public virtual void OnEntered(MainWindowViewModel context, IEvent ev, State oldState)
        {
            context._openCommand.NotifyCanExecuteChanged();
            context._runCommand.NotifyCanExecuteChanged();
            context.OnPropertyChanged(StateStatusChangedEventArgs);
        }
    }

    internal abstract record ProjectLoadedState(ProjectDto Project) : State
    {
        public override void OnExited(MainWindowViewModel context, IEvent ev, State newState)
        {
            if (newState is not ProjectLoadedState)
                context.OnPropertyChanged(ProgressBarVisibilityChangedEventArgs);
        }

        public override void OnEntered(MainWindowViewModel context, IEvent ev, State oldState)
        {
            base.OnEntered(context, ev, oldState);
            if (oldState is not ProjectLoadedState)
                context.OnPropertyChanged(ProgressBarVisibilityChangedEventArgs);
        }
    }

    internal sealed record ProjectNotLoadedState : State
    {
        internal static ProjectNotLoadedState Instance { get; } = new();

        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState) => ev switch
        {
            OpenEvent openEvent => Some(new LoadingState(openEvent.ProjectPath), out newState),
            _ => None(out newState)
        };

        public override void OnEntered(MainWindowViewModel context, IEvent ev, State oldState)
        {
            base.OnEntered(context, ev, oldState);
            context.OnPropertyChanged(TitleChangedEventArgs);
        }
    }

    internal sealed record LoadingState(string ProjectPath) : State
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState) => ev switch
        {
            CompleteEvent completeEvent => Some(new ReadyToRunState(ProjectPath, completeEvent.Project), out newState),
            CancelEvent => Some(ProjectNotLoadedState.Instance, out newState),
            _ => None(out newState)
        };

        public override void OnEntered(MainWindowViewModel context, IEvent ev, State oldState)
        {
            base.OnEntered(context, ev, oldState);
            context.OnPropertyChanged(TitleChangedEventArgs);
        }
    }

    internal sealed record ReadyToRunState(string ProjectPath, ProjectDto Project) : ProjectLoadedState(Project)
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState) => ev switch
        {
            OpenEvent openEvent => Some(new LoadingState(openEvent.ProjectPath), out newState),
            RunEvent => Some(new RunningState(ProjectPath, Project), out newState),
            _ => None(out newState)
        };
    }

    internal sealed record RunningState(string ProjectPath, ProjectDto Project) : ProjectLoadedState(Project)
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState) => ev switch
        {
            CompleteEvent completeEvent when Project == completeEvent.Project =>
                Some(new CompletedState(ProjectPath, Project), out newState),
            _ => None(out newState)
        };
    }

    internal sealed record CompletedState(string ProjectPath, ProjectDto Project) : ProjectLoadedState(Project)
    {
        public override bool TryCreateNewState(
            MainWindowViewModel context, IEvent ev, [MaybeNullWhen(false)] out State newState) => ev switch
        {
            RunEvent => Some(new RunningState(ProjectPath, Project), out newState),
            OpenEvent openEvent => Some(new LoadingState(openEvent.ProjectPath), out newState),
            _ => None(out newState)
        };

        public override void OnExiting(MainWindowViewModel context, IEvent ev, State newState) =>
            context.ProgressValue = 0.0;
    }
}
