using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel
{
    /// <summary>
    /// Gets the version from the assembly metadata.
    /// </summary>
    /// <param name="version">
    /// The version of the assembly, if metadata attributes are found; otherwise, the value is unspecified.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the assembly contains the version attribute; otherwise, <see langword="false" />.
    /// </returns>
    private static bool TryGetVersion([NotNullWhen(true)] out string? version)
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(MainWindowViewModel).Assembly;
        var attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute));
        var informationalVersions = attributes.OfType<AssemblyInformationalVersionAttribute>()
            .Select(it => it.InformationalVersion);
        string? informationalVersion = informationalVersions.FirstOrDefault(it => !string.IsNullOrWhiteSpace(it));
        if (informationalVersion is null)
        {
            version = assembly.GetName().Version?.ToString();
            return !string.IsNullOrWhiteSpace(version);
        }

        version = PrettifyCompoundVersion(informationalVersion);
        return true;
    }

    /// <summary>
    /// Reformats the given version string into a more concise format.
    /// </summary>
    /// <param name="compoundVersion">The input string containing the semver version and a commit hash concatenated with a plus sign.</param>
    /// <returns>The string that combines the semver version with the shorter commit hash in parentheses.</returns>
    /// <remarks>
    /// Example of the input string: "0.1.0+c92b6bb7177e32acac12028cbd13e68e11faa1dc".
    /// Example of the output string: "0.1.0 (c92b6bb)".
    /// </remarks>
    private static string PrettifyCompoundVersion(string compoundVersion)
    {
        var compoundVersionSpan = compoundVersion.AsSpan();
        Span<Range> ranges = stackalloc Range[3];
        int rangesWritten = compoundVersionSpan.Split(
            ranges, '+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rangesWritten < 2)
            return compoundVersion;

        var version = compoundVersionSpan[ranges[0]];
        var commit = compoundVersionSpan[ranges[1]];
        int commitLength = int.Min(commit.Length, 7);
        var shortCommit = commit[..commitLength];
        return $"{version.ToString()} ({shortCommit.ToString()})";
    }
}
