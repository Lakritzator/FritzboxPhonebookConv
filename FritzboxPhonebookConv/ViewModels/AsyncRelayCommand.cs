using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FritzboxPhonebookConv.ViewModels
{
    /// <summary>
    /// An async relay command that executes a <see cref="Func{Task}"/> and prevents
    /// re-entrant execution while the task is running.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) =>
            !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute().ConfigureAwait(true);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
