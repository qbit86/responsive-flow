using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ResponsiveFlow;

public sealed class MainWindowViewModel
{
    public string Title { get; } = CreateTitle();

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} {version}" : nameof(ResponsiveFlow);

    private static bool TryGetVersion([NotNullWhen(true)] out string? version)
    {
        var assembly = typeof(MainWindowViewModel).Assembly;
        var attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute));
        var informationalVersions =
            attributes.OfType<AssemblyInformationalVersionAttribute>().Select(it => it.InformationalVersion);
        string? informationalVersion = informationalVersions.FirstOrDefault(it => !string.IsNullOrWhiteSpace(it));
        version = informationalVersion ?? assembly.GetName().Version?.ToString();
        return !string.IsNullOrWhiteSpace(version);
    }
}
