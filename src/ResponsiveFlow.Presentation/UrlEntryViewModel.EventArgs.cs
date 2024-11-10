using System.ComponentModel;

namespace ResponsiveFlow;

public sealed partial class UrlEntryViewModel
{
    private static PropertyChangedEventArgs UrlStringChanged { get; } = new(nameof(UrlString));

    private static DataErrorsChangedEventArgs UrlStringDataErrorsChanged { get; } = new(nameof(UrlString));
}
