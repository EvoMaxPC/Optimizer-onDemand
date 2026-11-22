using System.Windows;
using EVO.Desktop.ViewModels;

namespace EVO.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
