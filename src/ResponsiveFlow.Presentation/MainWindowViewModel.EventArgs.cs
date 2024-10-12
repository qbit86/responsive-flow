using System.ComponentModel;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel
{
    private static PropertyChangedEventArgs ProgressBarVisibilityChangedEventArgs { get; } =
        new(nameof(ProgressBarVisibility));

    private static PropertyChangedEventArgs ProgressValueChangedEventArgs { get; } = new(nameof(ProgressValue));

    private static PropertyChangedEventArgs StateStatusChangedEventArgs { get; } = new(nameof(StateStatus));

    private static PropertyChangedEventArgs TitleChangedEventArgs { get; } = new(nameof(Title));
}
