using System.Windows.Input;

namespace EVO.Desktop.Commands;

/// <summary>Evento/comando simple para MVVM (sin librería externa).</summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Func<Task> _executeAsync;

    public DelegateCommand(Func<Task> executeAsync)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    }

    public bool CanExecute(object? parameter) => true;

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public void Execute(object? parameter)
    {
        _ = Task.Run(async () =>
        {
            await _executeAsync().ConfigureAwait(true);
        });
    }
}
