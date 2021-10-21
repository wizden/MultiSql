using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MultiSql.Common
{
    /// <summary>
    ///     Class to relay commands for ICommand buttons.
    /// </summary>
    public class RelayCommand : ICommand
    {

        #region Public

        #region Constructor

        public RelayCommand(Action<Object> execute, Func<Object, Boolean> canExecute = null)
        {
            this.execute    = execute;
            this.canExecute = canExecute;
        }

        public RelayCommand(Task execute, Func<Object, Boolean> canExecute = null)
        {
            executeTask     = execute;
            this.canExecute = canExecute;
        }

        #endregion

        #region Event

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        #endregion

        #region Method

        public Boolean CanExecute(Object parameter) => canExecute == null || canExecute(parameter);

        public void Execute(Object parameter)
        {
            execute(parameter);
        }

        public void ExecuteTask(Object param)
        {
            execute(param);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #endregion

        #region Private

        #region Field

        private readonly Func<Object, Boolean> canExecute;

        private readonly Action<Object> execute;

        private readonly Task executeTask;

        #endregion

        #endregion

    }
}
