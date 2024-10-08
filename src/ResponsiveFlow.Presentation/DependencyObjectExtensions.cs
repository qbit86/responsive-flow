using System;
using System.Windows;
using System.Windows.Media;

namespace ResponsiveFlow;

/// <seealso href="https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/rel/7.0.2/Microsoft.Toolkit.Uwp.UI/Extensions/DependencyObjectExtensions.cs#L105-L124" />
public static class DependencyObjectExtensions
{
    /// <summary>
    /// Searches for a first descendant element of a given type, using a depth-first search.
    /// </summary>
    /// <typeparam name="T">The type of elements to match.</typeparam>
    /// <param name="element">The root element.</param>
    /// <returns>The descendant that was found, or <see langword="null" />.</returns>
    public static T? FindDescendant<T>(this DependencyObject element)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(element);

        return FindDescendantUnchecked<T>(element);
    }

    private static T? FindDescendantUnchecked<T>(DependencyObject element)
        where T : DependencyObject
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childrenCount; ++i)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child is T result)
                return result;

            if (FindDescendantUnchecked<T>(child) is { } descendant)
                return descendant;
        }

        return null;
    }
}
