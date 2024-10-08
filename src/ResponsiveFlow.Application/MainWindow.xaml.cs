using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ResponsiveFlow;

public partial class MainWindow
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (MessageListView.FindDescendant<ScrollViewer>() is { } scrollViewer)
        {
            _viewModel.Messages.CollectionChanged += (o, args) =>
            {
                if (args.Action is NotifyCollectionChangedAction.Add)
                    scrollViewer.ScrollToBottom();
            };
        }

        _viewModel.Run();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        _viewModel.Shutdown();
        _viewModel.Dispose();
    }
}
