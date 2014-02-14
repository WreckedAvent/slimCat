#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICharacter.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    #region Usings

    using System.Windows.Media.Imaging;

    #endregion

    /// <summary>
    ///     Represents an F-list character.
    /// </summary>
    public interface ICharacter
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the character's avatar.
        /// </summary>
        BitmapImage Avatar { get; set; }

        /// <summary>
        ///     Gets or sets the character's gender.
        /// </summary>
        Gender Gender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether the character has an open report.
        /// </summary>
        bool HasReport { get; }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        ReportModel LastReport { get; set; }

        /// <summary>
        ///     Gets or sets the character's name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Gets or sets the character's status.
        /// </summary>
        StatusType Status { get; set; }

        /// <summary>
        ///     Gets or sets the character's status message.
        /// </summary>
        string StatusMessage { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the character is interesting to the current user.
        /// </summary>
        bool IsInteresting { get; set; }


        /// <summary>
        ///     Gets or sets a value indicating whether to ignore updates for this character.
        /// </summary>
        bool IgnoreUpdates { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Downloads the avatar from the server.
        /// </summary>
        void GetAvatar();

        #endregion
    }
}