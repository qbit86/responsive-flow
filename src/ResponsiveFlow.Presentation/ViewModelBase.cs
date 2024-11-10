using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ResponsiveFlow;

public abstract class ViewModelBase : ObservableObject
{
    /// <remarks>
    /// Requires the <see cref="PropertyChangedEventArgs" /> to be passed instead of being allocated on each call.
    /// </remarks>
    /// <returns><c>true</c> if the property was changed, <c>false</c> otherwise.</returns>
    protected bool TrySetProperty<T>(
        ref T field, T value, PropertyChangedEventArgs changed, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(changed);
        if (propertyName is not null)
            ArgumentOutOfRangeException.ThrowIfNotEqual(changed.PropertyName, propertyName, nameof(changed));

        return TrySetPropertyUnchecked(ref field, value, changed, EqualityComparer<T>.Default);
    }

    private bool TrySetPropertyUnchecked<T, TComparer>(
        ref T field, T value, PropertyChangedEventArgs changed, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        if (comparer.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(changed);
        return true;
    }
}
