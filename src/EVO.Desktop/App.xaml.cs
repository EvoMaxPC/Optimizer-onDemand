using System.Windows;
using EVO.Desktop.ViewModels;

namespace EVO.Desktop;

/// <summary>
/// Punto de entrada de la aplicación EVO.
/// </summary>
public partial class App : Application
{
    public App()
    {
        var viewModel = new MainViewModel();
        MainWindow = new MainWindow(viewModel);
    }
}
