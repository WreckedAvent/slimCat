#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RelayCommand.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Libraries
{
    #region Usings

    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Input;

    #endregion

    /// <summary>
    ///     The relay command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RelayCommand : ICommand
    {
        #region Fields

        private readonly Predicate<object> canExecute;

        private readonly Action<object> execute;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RelayCommand" /> class.
        /// </summary>
        /// <param name="execute">
        ///     The execute.
        /// </param>
        /// <param name="canExecute">
        ///     The can execute.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The can execute changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }

            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The can execute.
        /// </summary>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        /// <summary>
        ///     The execute.
        /// </summary>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        public void Execute(object parameter)
        {
            execute(parameter);
        }

        public override string ToString()
        {
            return string.Empty;
        }

        #endregion
    }
}