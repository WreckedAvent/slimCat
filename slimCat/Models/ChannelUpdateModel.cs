#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelUpdateModel.cs">
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
    ///     Used to represent an update about a channel
    /// </summary>
    public class ChannelUpdateModel : NotificationModel
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelUpdateModel" /> class.
        /// </summary>
        public ChannelUpdateModel(ChannelModel model, ChannelUpdateEventArgs e)
        {
            TargetChannel = model;
            Arguments = e;
        }

        public ChannelUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        public ChannelUpdateEventArgs Arguments { get; private set; }

        public ChannelModel TargetChannel { get; set; }

        public override Block View
        {
            get { return new ChannelUpdateView {DataContext = this}; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The to string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return Arguments.ToString();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetChannel = null;
            Arguments = null;
        }

        #endregion
    }

    /// <summary>
    ///     Represents arguments which have a channel as their direct object
    /// </summary>
    public abstract class ChannelUpdateEventArgs : EventArgs
    {
    }
}