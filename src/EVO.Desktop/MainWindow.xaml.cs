using System.Windows;
using EVO.Desktop.ViewModels;

namespace EVO.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new MainViewModel())
    {
    }

    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.RunAnalysisAsync();
    }
}
