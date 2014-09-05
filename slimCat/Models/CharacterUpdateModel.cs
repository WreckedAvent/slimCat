#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterUpdateModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Windows.Documents;
    using Views;

    #endregion

    /// <summary>
    ///     Used to represent an update about a character
    /// </summary>
    public partial class CharacterUpdateModel : NotificationModel
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterUpdateModel" /> class.
        /// </summary>
        public CharacterUpdateModel(ICharacter target, CharacterUpdateEventArgs e)
        {
            TargetCharacter = target;
            Arguments = e;
        }

        public CharacterUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        public CharacterUpdateEventArgs Arguments { get; private set; }

        /// <summary>
        ///     Gets the target character.
        /// </summary>
        public ICharacter TargetCharacter { get; private set; }

        public override Block View
        {
            get { return new CharacterUpdateView {DataContext = this}; }
        }

        #endregion

        #region Public Methods and Operators

        public override string ToString()
        {
            return TargetCharacter.Name + " " + Arguments;
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetCharacter = null;
            Arguments = null;
        }

        #endregion
    }

    /// <summary>
    ///     Represents updates which have a character as their direct object
    /// </summary>
    public abstract class CharacterUpdateEventArgs : EventArgs
    {
    }
}