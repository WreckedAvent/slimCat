// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListboxSelected.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The selector selected command behavior.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Libraries
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Commands;

    /// <summary>
    ///     The selector selected command behavior.
    /// </summary>
    public class SelectorSelectedCommandBehavior : CommandBehaviorBase<Selector>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorSelectedCommandBehavior"/> class.
        /// </summary>
        /// <param name="selectableObject">
        /// The selectable object.
        /// </param>
        public SelectorSelectedCommandBehavior(Selector selectableObject)
            : base(selectableObject)
        {
            selectableObject.SelectionChanged += this.OnSelectionChanged;
        }

        #endregion

        #region Methods

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CommandParameter = this.TargetObject.SelectedItem;
            this.ExecuteCommand();
        }

        #endregion
    }

    /// <summary>
    ///     The selected.
    /// </summary>
    public static class Selected
    {
        #region Static Fields

        /// <summary>
        ///     The command property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(Selected), new PropertyMetadata(OnSetCommandCallback));

        private static readonly DependencyProperty SelectedCommandBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "SelectedCommandBehavior", typeof(SelectorSelectedCommandBehavior), typeof(Selected), null);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get command.
        /// </summary>
        /// <param name="selector">
        /// The selector.
        /// </param>
        /// <returns>
        /// The <see cref="ICommand"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", 
            Justification = "Only works for selector")]
        public static ICommand GetCommand(Selector selector)
        {
            return selector.GetValue(CommandProperty) as ICommand;
        }

        /// <summary>
        /// The set command.
        /// </summary>
        /// <param name="selector">
        /// The selector.
        /// </param>
        /// <param name="command">
        /// The command.
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", 
            Justification = "Only works for selector")]
        public static void SetCommand(Selector selector, ICommand command)
        {
            selector.SetValue(CommandProperty, command);
        }

        #endregion

        #region Methods

        private static SelectorSelectedCommandBehavior GetOrCreateBehavior(Selector selector)
        {
            var behavior = selector.GetValue(SelectedCommandBehaviorProperty) as SelectorSelectedCommandBehavior;
            if (behavior == null)
            {
                behavior = new SelectorSelectedCommandBehavior(selector);
                selector.SetValue(SelectedCommandBehaviorProperty, behavior);
            }

            return behavior;
        }

        private static void OnSetCommandCallback(
            DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var selector = dependencyObject as Selector;
            if (selector != null)
            {
                GetOrCreateBehavior(selector).Command = e.NewValue as ICommand;
            }
        }

        #endregion
    }
}