using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace ResponsiveFlow;

public sealed partial class UrlEntryViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly List<string> _urlStringErrors = [];

    private Uri? _url;
    private string _urlString = string.Empty;

    public Uri? Url => _url;

    public string UrlString
    {
        get => _urlString;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            string oldValue = _urlString;
            if (!TrySetProperty(ref _urlString, value, UrlStringChanged))
                return;

            bool hadErrors = string.IsNullOrWhiteSpace(oldValue) || HasErrors;
            _urlStringErrors.Clear();
            if (!Uri.TryCreate(_urlString, UriKind.Absolute, out _url))
                _urlStringErrors.Add(nameof(Uri.TryCreate));

            if (HasErrors || hadErrors)
                ErrorsChanged?.Invoke(this, UrlStringDataErrorsChanged);
        }
    }

    public bool IsValid => !HasErrors && Url is not null;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is nameof(UrlString))
            return _urlStringErrors;
        return Array.Empty<string>();
    }

    public bool HasErrors => _urlStringErrors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
}
