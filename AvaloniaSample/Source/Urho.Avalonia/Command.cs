using System;
using System.Windows.Input;

namespace Urho.Avalonia
{
    public class Command : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public Command(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (_execute == null)
                return false;
            if (_canExecute != null && !_canExecute())
                return false;
            return true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    public class Command<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;


        public Command(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (_execute == null || !(parameter is T))
                return false;
            if (_canExecute != null && !_canExecute((T) parameter))
                return false;
            return true;
        }

        public void Execute(object parameter)
        {
            _execute((T) parameter);
        }
    }
}