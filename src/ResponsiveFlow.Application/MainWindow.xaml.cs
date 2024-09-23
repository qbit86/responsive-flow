using System;
using System.ComponentModel;

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

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        _viewModel.CancelRun();
    }
}
